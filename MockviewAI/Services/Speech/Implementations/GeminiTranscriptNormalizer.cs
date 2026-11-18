using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using MockviewAI.Services.Ai;
using MockviewAI.Services.Exceptions;
using MockviewAI.Services.KeyManagement.ProviderExtensions;
using MockviewAI.Services.Speech.Interfaces;

namespace MockviewAI.Services.Speech.Implementations
{
    /// <summary>
    /// [SPEECH]
    /// GeminiTranscriptNormalizer
    /// -- Gọi Gemini (generateContent) qua HttpClient "GeminiClient" để sửa lỗi bản chép giọng nói.
    /// -- Key do GeminiKeyHandler gắn qua header x-goog-api-key (không đặt trên URL).
    /// -- Model chính quá tải (503) / hết lượt (429) / không nhận yêu cầu (400, 404) / quá giờ: thử model dự phòng.
    /// -- Mỗi model có giới hạn thời gian riêng (NormalizeAttemptTimeoutSeconds), cả bước có giới hạn tổng (NormalizeTimeoutSeconds).
    /// -- Gửi thinkingLevel (mặc định "minimal") để model không "suy nghĩ" lâu; model không nhận tham số này (400)
    ///    thì gửi lại một lần không kèm. Mỗi lần gọi ghi log thời gian để đo độ trễ thật.
    /// -- Mọi lỗi còn lại được ném ra; SpeechToTextService sẽ dùng lại bản thô nên người dùng không bị chặn.
    /// </summary>
    public class GeminiTranscriptNormalizer : ITranscriptNormalizer
    {
        #region Fields
        public const string HttpClientName = "GeminiClient";

        // Tên model chỉ lấy từ cấu hình; chặn ký tự có thể làm đổi đường dẫn URL
        private static readonly Regex SafeModelName = new(@"^[A-Za-z0-9._-]+$", RegexOptions.Compiled);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GeminiKeyProvider _keys;
        private readonly ISpeechPromptBuilder _prompts;
        private readonly SpeechOptions _options;
        private readonly GeminiOptions _gemini;
        private readonly ILogger<GeminiTranscriptNormalizer> _logger;
        #endregion

        #region Constructor
        public GeminiTranscriptNormalizer(
            IHttpClientFactory httpClientFactory,
            GeminiKeyProvider keys,
            ISpeechPromptBuilder prompts,
            IOptions<SpeechOptions> options,
            IOptions<GeminiOptions> gemini,
            ILogger<GeminiTranscriptNormalizer> logger)
        {
            _httpClientFactory = httpClientFactory;
            _keys = keys;
            _prompts = prompts;
            _options = options.Value;
            _gemini = gemini.Value;
            _logger = logger;
        }
        #endregion

        #region ITranscriptNormalizer
        /// <inheritdoc />
        public async Task<string> NormalizeAsync(string rawText, string? question, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return string.Empty;
            if (!_keys.HasKeys)
                throw new SpeechException("Chưa cấu hình Gemini API key cho bước chuẩn hoá.") { IsConfigurationError = true };

            var models = ModelsToTry();
            if (models.Count == 0)
                throw new SpeechException("Chưa cấu hình model Gemini cho bước chuẩn hoá (Speech__NormalizerModel hoặc Gemini__Model).") { IsConfigurationError = true };

            // Giới hạn tổng cho cả bước: quá giờ thì dừng hẳn, dùng bản thô
            using var total = CancellationTokenSource.CreateLinkedTokenSource(ct);
            total.CancelAfter(TimeSpan.FromSeconds(Math.Max(3, _options.NormalizeTimeoutSeconds)));
            var perAttempt = TimeSpan.FromSeconds(Math.Max(2, _options.NormalizeAttemptTimeoutSeconds));

            SpeechException? lastError = null;
            foreach (var model in models)
            {
                if (total.IsCancellationRequested) break;

                // Giới hạn riêng cho model này: model chậm / treo không ăn hết thời gian của model dự phòng
                using var attempt = CancellationTokenSource.CreateLinkedTokenSource(total.Token);
                attempt.CancelAfter(perAttempt);
                try
                {
                    return await NormalizeWithModelAsync(model, rawText, question, ct, attempt.Token);
                }
                catch (SpeechException ex) when (ex.IsTransient && !ct.IsCancellationRequested)
                {
                    _logger.LogWarning("Gemini normalizer model {Model} unavailable or too slow ({Reason}), trying next model if any", model, ex.Message);
                    lastError = ex;
                }
            }
            throw lastError ?? new SpeechException("Bước chuẩn hoá tạm thời không khả dụng.") { IsTransient = true };
        }
        #endregion

        #region Request
        /// <summary>
        /// Gửi một yêu cầu chuẩn hoá tới một model. Lỗi có thể thử model khác được đánh dấu IsTransient.
        /// Model từ chối tham số thinkingConfig (HTTP 400) thì gửi lại một lần không kèm tham số đó.
        /// </summary>
        private async Task<string> NormalizeWithModelAsync(string model, string rawText, string? question, CancellationToken userCt, CancellationToken ct)
        {
            var thinkingLevel = (_options.NormalizerThinkingLevel ?? string.Empty).Trim();
            if (thinkingLevel.Length > 0)
            {
                try
                {
                    return await SendAsync(model, BuildBody(rawText, question, thinkingLevel), userCt, ct);
                }
                catch (SpeechException ex) when (ex.StatusCode == 400)
                {
                    _logger.LogWarning("Gemini model {Model} rejected thinkingLevel={Level}, retrying without it", model, thinkingLevel);
                }
            }
            return await SendAsync(model, BuildBody(rawText, question, null), userCt, ct);
        }

        /// <summary>Tạo nội dung yêu cầu generateContent; thinkingLevel null = không gửi thinkingConfig.</summary>
        private object BuildBody(string rawText, string? question, string? thinkingLevel)
        {
            var systemInstruction = new { parts = new[] { new { text = _prompts.NormalizerSystemPrompt } } };
            var contents = new[] { new { role = "user", parts = new[] { new { text = _prompts.BuildNormalizerUserPrompt(rawText, question) } } } };

            if (thinkingLevel is null)
                return new
                {
                    systemInstruction,
                    contents,
                    generationConfig = new { temperature = 0.0, maxOutputTokens = 4096 }   // sửa lỗi, không sáng tác
                };

            return new
            {
                systemInstruction,
                contents,
                generationConfig = new
                {
                    temperature = 0.0,
                    maxOutputTokens = 4096,                         // dư chỗ cho model có chế độ thinking; bản bị cắt sẽ bị IsSafeEdit loại
                    thinkingConfig = new { thinkingLevel }          // "minimal": gần như không suy nghĩ -> trả lời nhanh
                }
            };
        }

        /// <summary>Gọi API, ghi log mã trả về + thời gian, đổi lỗi thành SpeechException.</summary>
        private async Task<string> SendAsync(string model, object body, CancellationToken userCt, CancellationToken ct)
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var watch = Stopwatch.StartNew();
            try
            {
                using var response = await client.PostAsJsonAsync($"models/{model}:generateContent", body, ct);
                var json = await response.Content.ReadAsStringAsync(ct);
                int code = (int)response.StatusCode;

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Gemini normalizer HTTP {Status} for model {Model} after {Ms} ms: {Body}",
                        code, model, watch.ElapsedMilliseconds, json.Length > 300 ? json[..300] : json);
                    // 429/5xx: quá tải, hết lượt; 400/404: model không nhận yêu cầu -> đều thử model khác được
                    throw new SpeechException("Bước chuẩn hoá tạm thời không khả dụng.")
                    {
                        IsTransient = code is 400 or 404 or 429 or >= 500,
                        StatusCode = code
                    };
                }

                _logger.LogInformation("Gemini normalizer OK ({Model}) in {Ms} ms", model, watch.ElapsedMilliseconds);
                return ReadText(json);
            }
            catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException && !userCt.IsCancellationRequested)
            {
                _logger.LogWarning("Gemini normalizer model {Model} gave no answer after {Ms} ms", model, watch.ElapsedMilliseconds);
                throw new SpeechException("Bước chuẩn hoá quá giờ chờ hoặc mất kết nối.", ex) { IsTransient = true };
            }
        }

        /// <summary>
        /// Model chính (Speech:NormalizerModel, rỗng thì Gemini:Model) rồi model dự phòng
        /// (Speech:NormalizerFallbackModel, rỗng thì Gemini:Model); bỏ tên rỗng, trùng, không hợp lệ.
        /// </summary>
        private List<string> ModelsToTry()
        {
            var primary = string.IsNullOrWhiteSpace(_options.NormalizerModel) ? _gemini.Model : _options.NormalizerModel;
            var fallback = string.IsNullOrWhiteSpace(_options.NormalizerFallbackModel) ? _gemini.Model : _options.NormalizerFallbackModel;
            return new[] { primary, fallback }
                .Select(m => (m ?? string.Empty).Trim())
                .Where(m => m.Length > 0 && SafeModelName.IsMatch(m))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Ghép các đoạn văn bản trả lời của Gemini; bỏ qua đoạn "suy nghĩ" (thought) của model có chế độ thinking.
        /// </summary>
        private static string ReadText(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                    return string.Empty;
                if (!candidates[0].TryGetProperty("content", out var content) || !content.TryGetProperty("parts", out var parts))
                    return string.Empty;

                var sb = new StringBuilder();
                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("thought", out var thought) && thought.ValueKind == JsonValueKind.True) continue;
                    if (part.TryGetProperty("text", out var text)) sb.Append(text.GetString());
                }
                return sb.ToString().Trim().Trim('"', '“', '”').Trim();
            }
            catch (JsonException ex)
            {
                throw new SpeechException("Bước chuẩn hoá trả về dữ liệu không đọc được.", ex);
            }
        }
        #endregion
    }
}
