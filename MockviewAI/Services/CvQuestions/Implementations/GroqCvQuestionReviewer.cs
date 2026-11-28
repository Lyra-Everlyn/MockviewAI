using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using MockviewAI.Models.CvQuestions;
using MockviewAI.Services.CvQuestions.Interfaces;
using MockviewAI.Services.Exceptions;
using MockviewAI.Services.KeyManagement.ProviderExtensions;

namespace MockviewAI.Services.CvQuestions.Implementations
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// Người kiểm duyệt: gọi Groq (model khác họ Gemini) qua HttpClient "GroqClient" (xoay vòng key),
    /// endpoint OpenAI-compatible chat/completions, chế độ JSON, temperature 0.
    /// Groq lỗi -> ném CvQuestionException để pipeline không cho câu nào qua (an toàn hơn).
    /// </summary>
    public class GroqCvQuestionReviewer : ICvQuestionReviewer
    {
        #region Fields
        private const string HttpClientName = "GroqClient";
        private const int MaxErrorLogChars = 300;

        /// <summary>Đủ cho 3–4 kết quả duyệt dạng JSON; gói Groq miễn phí của qwen chỉ cho 1.000 token đầu ra / phút (xin 2.000 bị 429).</summary>
        private const int MaxOutputTokens = 900;

        /// <summary>
        /// Model gpt-oss "suy nghĩ" trước khi trả lời (tốn token đầu ra): 900 thì bị cắt giữa JSON, thiếu kết quả.
        /// Không giảm mức suy nghĩ vì đo 27/11 thấy reasoning_effort=low cho qua cả câu hỏi riêng tư.
        /// </summary>
        private const int MaxOutputTokensReasoning = 2000;
        private static readonly Regex SafeModelName = new(@"^[A-Za-z0-9._/-]+$", RegexOptions.Compiled);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GroqKeyProvider _keys;
        private readonly ICvQuestionPromptBuilder _prompts;
        private readonly IReviewerCooldown _cooldown;
        private readonly CvQuestionOptions _options;
        private readonly ILogger<GroqCvQuestionReviewer> _logger;
        #endregion

        #region Constructor
        public GroqCvQuestionReviewer(
            IHttpClientFactory httpClientFactory,
            GroqKeyProvider keys,
            ICvQuestionPromptBuilder prompts,
            IReviewerCooldown cooldown,
            IOptions<CvQuestionOptions> options,
            ILogger<GroqCvQuestionReviewer> logger)
        {
            _httpClientFactory = httpClientFactory;
            _keys = keys;
            _prompts = prompts;
            _cooldown = cooldown;
            _options = options.Value;
            _logger = logger;
        }
        #endregion

        #region ICvQuestionReviewer
        /// <summary>Model đã duyệt ở lần gần nhất (model chính, hoặc một model dự phòng khi các model trước hết lượt).</summary>
        public string Model { get; private set; } = string.Empty;

        public bool IsAvailable => AllModels().Any(m => !_cooldown.IsCooling(m));

        public async Task<List<CvQuestionReview>> ReviewAsync(CvQuestionContext ctx, IReadOnlyList<CvQuestionDraft> drafts, CancellationToken ct)
        {
            if (drafts.Count == 0) return new List<CvQuestionReview>();
            if (!_keys.HasKeys)
                throw new CvQuestionException("Thiếu Groq API key.");

            var all = AllModels();
            if (all.Count == 0)
                throw new CvQuestionException("Tên model kiểm duyệt không hợp lệ (CvQuestions:ReviewerModel).");
            // Bỏ qua model vừa hết lượt; tất cả đang nghỉ (hết lượt ngay trong lúc soạn) thì báo lỗi luôn, không gọi Groq
            var models = all.Where(m => !_cooldown.IsCooling(m)).ToList();
            if (models.Count == 0)
                throw new CvQuestionException("Mọi model kiểm duyệt đang tạm nghỉ vì vừa hết lượt.") { StatusCode = 429, IsTransient = true };

            string userPrompt = _prompts.BuildReviewerPrompt(ctx, drafts);
            CvQuestionException? lastError = null;
            foreach (var model in models)
            {
                try
                {
                    var reviews = await ReviewWithModelAsync(model, userPrompt, drafts.Count, ct);
                    Model = model;
                    return reviews;
                }
                // Groq miễn phí giới hạn token / phút RIÊNG từng model -> hết lượt, model bị gỡ hoặc quá tải thì thử model dự phòng
                catch (CvQuestionException ex) when (ex.IsTransient)
                {
                    _logger.LogWarning("CV question reviewer model {Model} unavailable (HTTP {Status}), trying next model if any", model, ex.StatusCode);
                    lastError = ex;
                }
            }
            throw lastError!;
        }
        #endregion

        #region Request
        private async Task<List<CvQuestionReview>> ReviewWithModelAsync(string model, string userPrompt, int count, CancellationToken ct)
        {
            var body = new Dictionary<string, object>
            {
                ["model"] = model,
                ["temperature"] = 0,
                ["max_tokens"] = MaxOutputTokensFor(model),
                ["response_format"] = new { type = "json_object" },
                ["messages"] = new[]
                {
                    new { role = "system", content = _prompts.ReviewerSystemPrompt },
                    new { role = "user", content = userPrompt }
                }
            };

            var client = _httpClientFactory.CreateClient(HttpClientName);
            var watch = Stopwatch.StartNew();
            string json;
            try
            {
                using var response = await client.PostAsJsonAsync("chat/completions", body, ct);
                json = await response.Content.ReadAsStringAsync(ct);
                if (!response.IsSuccessStatusCode)
                {
                    int code = (int)response.StatusCode;
                    _logger.LogWarning("Groq CV review HTTP {Status} for model {Model} after {Ms} ms: {Body}",
                        code, model, watch.ElapsedMilliseconds, json.Length > MaxErrorLogChars ? json[..MaxErrorLogChars] : json);
                    if (code is 413 or 429) StartCooldown(model, response.Headers.RetryAfter);
                    // 400/404 (model bị gỡ / không nhận tham số), 413 (yêu cầu vượt giới hạn token của model), 429 (hết lượt), 5xx (quá tải)
                    // -> thử model dự phòng; 401 (sai khoá) thì không
                    throw new CvQuestionException($"Groq trả HTTP {code}.") { StatusCode = code, IsTransient = code is 400 or 404 or 413 or 429 or >= 500 };
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning("Groq CV review network error ({Model}) after {Ms} ms", model, watch.ElapsedMilliseconds);
                throw new CvQuestionException("Không kết nối được Groq.", ex) { IsTransient = true };
            }

            var reviews = CvReviewParser.Parse(ReadContent(json), count);
            _logger.LogInformation("Groq CV review OK ({Model}) in {Ms} ms: {Pass} pass, {Fix} fix, {Reject} reject",
                model, watch.ElapsedMilliseconds,
                reviews.Count(r => r.Verdict == CvQuestionReview.Pass),
                reviews.Count(r => r.Verdict == CvQuestionReview.Fix),
                reviews.Count(r => r.Verdict == CvQuestionReview.Reject));
            return reviews;
        }
        #endregion

        #region Helpers
        /// <summary>Model chính rồi các model dự phòng; bỏ tên rỗng / không hợp lệ / trùng.</summary>
        private List<string> AllModels() =>
            new[] { _options.ReviewerModel }
                .Concat(_options.ReviewerFallbackList)
                .Select(m => (m ?? string.Empty).Trim())
                .Where(m => m.Length > 0 && SafeModelName.IsMatch(m))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        /// <summary>Thời gian nghỉ = retry-after của Groq (nếu có), không quá CvQuestions:ReviewerCooldownSeconds.</summary>
        private void StartCooldown(string model, RetryConditionHeaderValue? retryAfter)
        {
            int limit = _options.ReviewerCooldownLimit;
            if (limit == 0) return;
            var wait = retryAfter?.Delta
                       ?? (retryAfter?.Date is { } date ? date - DateTimeOffset.UtcNow : (TimeSpan?)null)
                       ?? TimeSpan.FromSeconds(limit);
            wait = TimeSpan.FromSeconds(Math.Clamp(wait.TotalSeconds, 1, limit));
            _cooldown.Trip(model, wait);
            _logger.LogWarning("CV question reviewer model {Model} paused for {Seconds:0} s (rate limit)", model, wait.TotalSeconds);
        }

        private static int MaxOutputTokensFor(string model) =>
            model.Contains("gpt-oss", StringComparison.OrdinalIgnoreCase) ? MaxOutputTokensReasoning : MaxOutputTokens;

        /// <summary>choices[0].message.content; cấu trúc lạ thì trả rỗng (mọi câu thành reject).</summary>
        private static string ReadContent(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array
                    && choices.GetArrayLength() > 0
                    && choices[0].TryGetProperty("message", out var message)
                    && message.TryGetProperty("content", out var content)
                    && content.ValueKind == JsonValueKind.String)
                    return content.GetString() ?? string.Empty;
            }
            catch (JsonException)
            {
            }
            return string.Empty;
        }
        #endregion
    }
}
