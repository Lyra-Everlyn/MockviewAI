using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using MockviewAI.Models.CvQuestions;
using MockviewAI.Services.Ai;
using MockviewAI.Services.CvQuestions.Interfaces;
using MockviewAI.Services.Exceptions;
using MockviewAI.Services.KeyManagement.ProviderExtensions;

namespace MockviewAI.Services.CvQuestions.Implementations
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// Người soạn: gọi Gemini (chế độ JSON) qua HttpClient "GeminiClient" (xoay vòng key), cách gọi giống GeminiCvParsingService.
    /// Model lỗi tạm thời thì thử model dự phòng; thời gian do CancellationToken của CvQuestionPipeline giới hạn.
    /// Log chỉ ghi model, mã HTTP, thời gian: không ghi CV, câu hỏi hay key.
    /// </summary>
    public class GeminiCvQuestionGenerator : ICvQuestionGenerator
    {
        #region Fields
        private const string HttpClientName = "GeminiClient";
        private const int MaxErrorLogChars = 300;
        private static readonly string[] DefaultFallbackModels = { "gemini-3.5-flash-lite" };
        private static readonly Regex SafeModelName = new(@"^[A-Za-z0-9._-]+$", RegexOptions.Compiled);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GeminiKeyProvider _keys;
        private readonly ICvQuestionPromptBuilder _prompts;
        private readonly CvQuestionOptions _options;
        private readonly GeminiOptions _gemini;
        private readonly ILogger<GeminiCvQuestionGenerator> _logger;
        #endregion

        #region Constructor
        public GeminiCvQuestionGenerator(
            IHttpClientFactory httpClientFactory,
            GeminiKeyProvider keys,
            ICvQuestionPromptBuilder prompts,
            IOptions<CvQuestionOptions> options,
            IOptions<GeminiOptions> gemini,
            ILogger<GeminiCvQuestionGenerator> logger)
        {
            _httpClientFactory = httpClientFactory;
            _keys = keys;
            _prompts = prompts;
            _options = options.Value;
            _gemini = gemini.Value;
            _logger = logger;
        }
        #endregion

        #region ICvQuestionGenerator
        public async Task<CvGenerationResult> GenerateAsync(CvQuestionContext ctx, int count, IReadOnlyList<string>? fixNotes, CancellationToken ct)
        {
            if (!_keys.HasKeys)
                throw new CvQuestionException("Thiếu Gemini API key.");
            var models = ModelsToTry();
            if (models.Count == 0)
                throw new CvQuestionException("Chưa cấu hình model Gemini hợp lệ.");

            string userPrompt = _prompts.BuildGeneratorPrompt(ctx, count, fixNotes);
            CvQuestionException? lastError = null;
            foreach (var model in models)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var text = await GenerateWithModelAsync(model, userPrompt, ct);
                    var drafts = CvQuestionParser.Parse(text, ctx.PositionKey, ctx.InterviewerKey, out var problem);
                    if (problem != null)
                        _logger.LogWarning("Gemini CV questions only partly readable ({Model}): {Problem}, got {Count}/{Wanted} ({Chars} chars)",
                            model, problem, drafts.Count, count, text.Length);   // chỉ ghi lý do + độ dài, không ghi nội dung

                    // Đọc ra 0 câu (hiếm, do AI trả sai định dạng) -> gọi lại đúng 1 lần cùng model; thời gian vẫn do pipeline giới hạn
                    if (drafts.Count == 0)
                    {
                        text = await GenerateWithModelAsync(model, userPrompt, ct);
                        drafts = CvQuestionParser.Parse(text, ctx.PositionKey, ctx.InterviewerKey, out problem);
                        _logger.LogInformation("Gemini CV questions retry ({Model}): got {Count}/{Wanted}{Problem}",
                            model, drafts.Count, count, problem == null ? "" : " - " + problem);
                    }
                    return new CvGenerationResult(drafts, model);
                }
                catch (CvQuestionException ex) when (ex.IsTransient)
                {
                    _logger.LogWarning("CV question generator model {Model} unavailable (HTTP {Status}), trying next model if any", model, ex.StatusCode);
                    lastError = ex;
                }
            }
            throw lastError ?? new CvQuestionException("Không model Gemini nào soạn được câu hỏi.");
        }
        #endregion

        #region Request
        /// <summary>
        /// Gửi kèm khuôn JSON (responseSchema) + thinkingLevel. Model từ chối (400) thì lùi dần:
        /// bỏ thinkingLevel -> bỏ luôn khuôn JSON (bộ đọc vẫn chịu lỗi và vá được lỗi thiếu ngoặc kép).
        /// </summary>
        private async Task<string> GenerateWithModelAsync(string model, string userPrompt, CancellationToken ct)
        {
            var thinkingLevel = (_options.ThinkingLevel ?? string.Empty).Trim();
            if (thinkingLevel.Length > 0)
            {
                try
                {
                    return await SendAsync(model, BuildBody(userPrompt, thinkingLevel, useSchema: true), ct);
                }
                catch (CvQuestionException ex) when (ex.StatusCode == 400)
                {
                    _logger.LogWarning("Gemini model {Model} rejected thinkingLevel={Level}, retrying without it", model, thinkingLevel);
                }
            }
            try
            {
                return await SendAsync(model, BuildBody(userPrompt, null, useSchema: true), ct);
            }
            catch (CvQuestionException ex) when (ex.StatusCode == 400)
            {
                _logger.LogWarning("Gemini model {Model} rejected responseSchema, retrying without it", model);
            }
            return await SendAsync(model, BuildBody(userPrompt, null, useSchema: false), ct);
        }

        private object BuildBody(string userPrompt, string? thinkingLevel, bool useSchema)
        {
            var systemInstruction = new { parts = new[] { new { text = _prompts.GeneratorSystemPrompt } } };
            var contents = new[] { new { role = "user", parts = new[] { new { text = userPrompt } } } };

            // temperature 0.6: đủ sáng tạo để mỗi buổi khác nhau; luật code + người kiểm duyệt giữ câu đúng chuẩn
            var config = new Dictionary<string, object>
            {
                ["temperature"] = 0.6,
                ["maxOutputTokens"] = 8192,
                ["responseMimeType"] = "application/json"
            };
            if (useSchema) config["responseSchema"] = CvQuestionResponseSchema.Value;
            if (thinkingLevel is not null) config["thinkingConfig"] = new { thinkingLevel };

            return new { systemInstruction, contents, generationConfig = config };
        }

        private async Task<string> SendAsync(string model, object body, CancellationToken ct)
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
                    _logger.LogWarning("Gemini CV question HTTP {Status} for model {Model} after {Ms} ms: {Body}",
                        code, model, watch.ElapsedMilliseconds, json.Length > MaxErrorLogChars ? json[..MaxErrorLogChars] : json);
                    // 400 (tham số), 404 (model không có), 429 (hết lượt), 5xx (quá tải) -> thử model khác
                    throw new CvQuestionException($"Gemini trả HTTP {code}.")
                    {
                        StatusCode = code,
                        IsTransient = code is 400 or 404 or 429 or >= 500
                    };
                }

                _logger.LogInformation("Gemini CV questions OK ({Model}) in {Ms} ms", model, watch.ElapsedMilliseconds);
                return ReadText(json);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning("Gemini CV question model {Model} network error after {Ms} ms", model, watch.ElapsedMilliseconds);
                throw new CvQuestionException("Không kết nối được Gemini.", ex) { IsTransient = true };
            }
        }

        /// <summary>Thứ tự: CvQuestions:GeneratorModel -> Gemini:Model -> Gemini:FallbackModels.</summary>
        private List<string> ModelsToTry() =>
            new[] { _options.GeneratorModel, _gemini.Model }
                .Concat(_gemini.FallbackModels ?? DefaultFallbackModels)
                .Select(m => (m ?? string.Empty).Trim())
                .Where(m => m.Length > 0 && SafeModelName.IsMatch(m))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        #endregion

        #region Helpers
        /// <summary>Ghép phần văn bản Gemini trả về, bỏ phần "thought".</summary>
        private static string ReadText(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0
                    || !candidates[0].TryGetProperty("content", out var content) || !content.TryGetProperty("parts", out var parts))
                    throw new CvQuestionException("Gemini không trả về nội dung.") { IsTransient = true };

                var sb = new StringBuilder();
                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("thought", out var thought) && thought.ValueKind == JsonValueKind.True) continue;
                    if (part.TryGetProperty("text", out var text)) sb.Append(text.GetString());
                }
                return sb.ToString();
            }
            catch (JsonException ex)
            {
                throw new CvQuestionException("Gemini trả dữ liệu không đọc được.", ex) { IsTransient = true };
            }
        }
        #endregion
    }
}
