using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using MockviewAI.Models.Ai;
using MockviewAI.Services.Ai.Interfaces;
using MockviewAI.Services.Exceptions;
using MockviewAI.Services.KeyManagement.ProviderExtensions;

namespace MockviewAI.Services.Ai.Implementations
{
    /// <summary>
    /// [AI-SCORING] 
    /// Gửi yêu cầu chấm điểm tới Gemini qua HttpClient "GeminiClient" (xoay vòng key).
    /// Gửi kèm thinkingLevel để rút ngắn thời gian chấm; model lỗi tạm thời thì thử model dự phòng.
    /// </summary>
    public class GeminiAiScoringService : IAiScoringService
    {
        #region Fields
        public const string HttpClientName = "GeminiClient";
        private static readonly string[] DefaultFallbackModels = { "gemini-3.5-flash-lite" };

        /// <summary>Dấu hiệu tên model không dùng để chấm văn bản (giọng nói, ảnh, nhúng...).</summary>
        private static readonly string[] NonTextModelMarkers = { "-tts", "-live", "native-audio", "transcribe", "image", "embedding", "imagen", "veo", "lyria", "omni", "robotics" };

        /// <summary>Tên model chỉ lấy từ cấu hình; chặn ký tự có thể làm đổi đường dẫn URL.</summary>
        private static readonly Regex SafeModelName = new(@"^[A-Za-z0-9._-]+$", RegexOptions.Compiled);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GeminiKeyProvider _keys;
        private readonly GeminiOptions _options;
        private readonly IScoringPromptBuilder _promptBuilder;
        private readonly IAiResponseParser _parser;
        private readonly ILogger<GeminiAiScoringService> _logger;
        #endregion

        #region Constructor
        public GeminiAiScoringService(
            IHttpClientFactory httpClientFactory,
            GeminiKeyProvider keys,
            IOptions<GeminiOptions> options,
            IScoringPromptBuilder promptBuilder,
            IAiResponseParser parser,
            ILogger<GeminiAiScoringService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _keys = keys;
            _options = options.Value;
            _promptBuilder = promptBuilder;
            _parser = parser;
            _logger = logger;
        }
        #endregion

        #region IAiScoringService
        public async Task<ContentScoreResult> ScoreContentAsync(ScoreRequest request, CancellationToken ct = default)
        {
            if (!_keys.HasKeys)
                throw new AiScoringException("AI chưa được cấu hình (thiếu Gemini__ApiKeys__0).", isConfigurationError: true);
            if (request.Answers.Count == 0)
                throw new AiScoringException("Chưa có câu trả lời nào để chấm.");
            if (!SafeModelName.IsMatch(_options.Model ?? ""))
                throw new AiScoringException("Tên model Gemini không hợp lệ (Gemini:Model).", isConfigurationError: true);
            if (IsNonTextModel(_options.Model!))
                throw new AiScoringException($"Model AI '{_options.Model}' không dùng để chấm văn bản. Quản trị viên cần đổi Gemini:Model.", isConfigurationError: true);

            string systemPrompt = _promptBuilder.SystemPrompt;
            string userPrompt = _promptBuilder.BuildUserPrompt(request);
            var http = _httpClientFactory.CreateClient(HttpClientName);

            AiScoringException? lastError = null;
            foreach (var model in ModelsToTry())
            {
                var watch = Stopwatch.StartNew();
                try
                {
                    var result = await ScoreWithModelAsync(http, model, systemPrompt, userPrompt, ct);
                    _logger.LogInformation("Scored {Answers} answers with {Model} (thinking={Thinking}) in {Ms} ms, {KeyCount} key(s) in rotation",
                        request.Answers.Count, model, _options.ThinkingLevel, watch.ElapsedMilliseconds, _keys.KeyCount);
                    return result;
                }
                catch (AiScoringException ex) when ((ex.IsTransient || ex.CanTryOtherModel) && !ct.IsCancellationRequested)
                {
                    _logger.LogWarning("Gemini model {Model} unavailable after {Ms} ms ({Reason}). Trying the next model if any.",
                        model, watch.ElapsedMilliseconds, ex.Message);
                    lastError = ex;
                }
            }
            throw lastError ?? new AiScoringException("AI tạm thời không phản hồi. Vui lòng thử lại sau.") { IsTransient = true };
        }
        #endregion

        #region Request
        /// <summary>Model chính trước, rồi các model dự phòng hợp lệ (bỏ trùng, bỏ model không phải văn bản).</summary>
        private List<string> ModelsToTry()
        {
            var models = new List<string> { _options.Model! };
            foreach (var m in _options.FallbackModels ?? DefaultFallbackModels)
            {
                var name = (m ?? "").Trim();
                if (name.Length == 0 || !SafeModelName.IsMatch(name) || IsNonTextModel(name)) continue;
                if (models.Contains(name, StringComparer.OrdinalIgnoreCase)) continue;
                models.Add(name);
            }
            return models;
        }

        /// <summary>Gửi đầy đủ (JSON mode + thinkingLevel). Lỗi 400 không do key thì gửi lại một lần dạng tối giản.</summary>
        private async Task<ContentScoreResult> ScoreWithModelAsync(HttpClient http, string model, string systemPrompt, string userPrompt, CancellationToken ct)
        {
            using (var response = await SendAsync(http, model, systemPrompt, userPrompt, minimal: false, ct))
            {
                if (response.IsSuccessStatusCode)
                    return _parser.Parse(await response.Content.ReadAsStringAsync(ct));

                var reason = await ReadErrorReasonAsync(response, ct);
                _logger.LogWarning("Gemini returned HTTP {Status} for model {Model}: {Reason}", (int)response.StatusCode, model, reason);

                bool retryMinimal = response.StatusCode == HttpStatusCode.BadRequest
                                    && !reason.Contains("API key", StringComparison.OrdinalIgnoreCase);
                if (!retryMinimal)
                    throw ToFriendlyException(response.StatusCode, reason, model);
            }

            using var retry = await SendAsync(http, model, systemPrompt, userPrompt, minimal: true, ct);
            if (retry.IsSuccessStatusCode)
            {
                _logger.LogInformation("Gemini model {Model} succeeded with the minimal request.", model);
                return _parser.Parse(await retry.Content.ReadAsStringAsync(ct));
            }

            var retryReason = await ReadErrorReasonAsync(retry, ct);
            _logger.LogWarning("Gemini minimal request also failed: HTTP {Status} for model {Model}: {Reason}", (int)retry.StatusCode, model, retryReason);
            throw ToFriendlyException(retry.StatusCode, retryReason, model);
        }

        /// <summary>Key do GeminiKeyHandler gắn; nội dung JsonContent đọc lại được khi handler gửi lại.</summary>
        private async Task<HttpResponseMessage> SendAsync(HttpClient http, string model, string systemPrompt, string userPrompt, bool minimal, CancellationToken ct)
        {
            var thinkingLevel = (_options.ThinkingLevel ?? string.Empty).Trim();

            object generationConfig;
            if (minimal)
                generationConfig = new { maxOutputTokens = _options.MaxTokens };
            else if (thinkingLevel.Length == 0)
                generationConfig = new { maxOutputTokens = _options.MaxTokens, temperature = 0.2, responseMimeType = "application/json" };
            else
                generationConfig = new { maxOutputTokens = _options.MaxTokens, temperature = 0.2, responseMimeType = "application/json", thinkingConfig = new { thinkingLevel } };

            var body = new
            {
                systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
                contents = new[] { new { role = "user", parts = new[] { new { text = userPrompt } } } },
                generationConfig
            };

            using var msg = new HttpRequestMessage(HttpMethod.Post, $"models/{model}:generateContent")
            {
                Content = JsonContent.Create(body)
            };

            try
            {
                return await http.SendAsync(msg, ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Gemini request failed (network/timeout) for model {Model}", model);
                throw new AiScoringException("Không kết nối được tới AI hoặc AI phản hồi quá lâu. Vui lòng thử lại.", ex) { IsTransient = true };
            }
        }
        #endregion

        #region Helpers
        private static bool IsNonTextModel(string model) =>
            NonTextModelMarkers.Any(m => model.Contains(m, StringComparison.OrdinalIgnoreCase));

        /// <summary>Đổi mã HTTP thành thông báo tiếng Việt. 429 ở đây = handler đã thử hết các key.</summary>
        private static AiScoringException ToFriendlyException(HttpStatusCode status, string reason, string model)
        {
            int code = (int)status;
            if (status == HttpStatusCode.NotFound)
                return new AiScoringException($"AI chưa được cấu hình đúng: không tìm thấy model '{model}' (mã 404).", isConfigurationError: true) { CanTryOtherModel = true };
            if (status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                || (status == HttpStatusCode.BadRequest && reason.Contains("API key", StringComparison.OrdinalIgnoreCase)))
                return new AiScoringException($"AI chưa được cấu hình đúng: API key không hợp lệ hoặc chưa được cấp quyền (mã {code}).", isConfigurationError: true);
            if (status == HttpStatusCode.BadRequest)
                return new AiScoringException($"Model AI '{model}' không nhận yêu cầu chấm điểm (mã 400).", isConfigurationError: true) { CanTryOtherModel = true };
            if (status == HttpStatusCode.TooManyRequests)
                return new AiScoringException("AI đang quá tải hoặc đã hết lượt dùng miễn phí (mã 429). Vui lòng thử lại sau ít phút.") { IsTransient = true };
            if (code >= 500)
                return new AiScoringException($"AI tạm thời không phản hồi (mã {code}). Vui lòng thử lại sau.") { IsTransient = true };
            return new AiScoringException($"AI không xử lý được yêu cầu (mã {code}). Vui lòng thử lại.");
        }

        /// <summary>Đọc lý do lỗi trong body Gemini; không đọc được JSON thì trả nguyên văn (cắt ngắn).</summary>
        private static async Task<string> ReadErrorReasonAsync(HttpResponseMessage response, CancellationToken ct)
        {
            string body;
            try
            {
                body = await response.Content.ReadAsStringAsync(ct);
            }
            catch (Exception)
            {
                return "(could not read error body)";
            }
            if (string.IsNullOrWhiteSpace(body)) return "(empty error body)";

            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0) root = root[0];
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.Object)
                {
                    var status = error.TryGetProperty("status", out var s) ? s.GetString() : "";
                    var message = error.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";
                    return Truncate($"{status} {message}".Trim(), 500);
                }
            }
            catch (JsonException) { }
            return Truncate(body.ReplaceLineEndings(" "), 500);
        }

        private static string Truncate(string text, int max) => text.Length > max ? text[..max] + "..." : text;
        #endregion
    }
}
