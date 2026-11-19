using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MockviewAI.Models.Ai;
using MockviewAI.Services.Ai.Interfaces;
using MockviewAI.Services.Exceptions;
using MockviewAI.Services.KeyManagement.ProviderExtensions;

namespace MockviewAI.Services.Ai.Implementations
{
    /// <summary>
    /// [AI-MODULE]
    /// GeminiAiScoringService
    /// -- Chỉ lo việc GỬI REQUEST chấm điểm tới Gemini (generateContent).
    /// -- Soạn prompt do IScoringPromptBuilder, đọc kết quả do IAiResponseParser.
    /// -- [TASK 2] Dùng HttpClient "GeminiClient": API key do GeminiKeyHandler gắn và XOAY VÒNG
    ///    (Gemini__ApiKeys__0..N, hoặc Gemini__ApiKey nếu chỉ có 1 key). Class này không đọc, không ghi log key.
    /// -- Key lỗi quota (429/401/403): handler tự đổi key và gửi lại. Hết key / quá tải: thử model dự phòng.
    /// </summary>
    public class GeminiAiScoringService : IAiScoringService
    {
        public const string HttpClientName = "GeminiClient";
        private static readonly string[] DefaultFallbackModels = { "gemini-3.5-flash-lite" };
        private static readonly string[] NonTextModelMarkers = { "-tts", "-live", "native-audio", "transcribe", "image", "embedding", "imagen", "veo", "lyria", "omni", "robotics" };
        private static readonly Regex SafeModelName = new(@"^[A-Za-z0-9._-]+$", RegexOptions.Compiled);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GeminiKeyProvider _keys;
        private readonly GeminiOptions _options;
        private readonly IScoringPromptBuilder _promptBuilder;
        private readonly IAiResponseParser _parser;
        private readonly ILogger<GeminiAiScoringService> _logger;

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

        #region IAiScoringService
        /// <inheritdoc />
        public async Task<ContentScoreResult> ScoreContentAsync(ScoreRequest request, CancellationToken ct = default)
        {
            if (!_keys.HasKeys)
                throw new AiScoringException("AI chưa được cấu hình (thiếu Gemini__ApiKeys__0 hoặc Gemini__ApiKey).", isConfigurationError: true);
            if (request.Answers.Count == 0)
                throw new AiScoringException("Chưa có câu trả lời nào để chấm.");

            // Tên model chỉ lấy từ cấu hình; chặn ký tự có thể làm đổi đường dẫn URL
            if (!SafeModelName.IsMatch(_options.Model ?? ""))
                throw new AiScoringException("Tên model Gemini không hợp lệ (Gemini:Model).", isConfigurationError: true);

            // Chỉ model sinh VĂN BẢN mới chấm được (model giọng nói/ảnh/embedding không trả JSON)
            if (IsNonTextModel(_options.Model!))
            {
                _logger.LogError("Gemini:Model '{Model}' is not a text-generation model.", _options.Model);
                throw new AiScoringException(
                    $"Model AI '{_options.Model}' không dùng để chấm văn bản. Quản trị viên cần đổi Gemini:Model.",
                    isConfigurationError: true);
            }

            string systemPrompt = _promptBuilder.SystemPrompt;
            string userPrompt = _promptBuilder.BuildUserPrompt(request);
            var http = _httpClientFactory.CreateClient(HttpClientName);

            _logger.LogInformation("Scoring with Gemini ({KeyCount} key(s) in rotation)", _keys.KeyCount);

            AiScoringException? lastError = null;
            foreach (var model in ModelsToTry())
            {
                try
                {
                    return await ScoreWithModelAsync(http, model, systemPrompt, userPrompt, ct);
                }
                catch (AiScoringException ex) when ((ex.IsTransient || ex.CanTryOtherModel) && !ct.IsCancellationRequested)
                {
                    // Quá tải / hết lượt ở mọi key / hết giờ chờ / model không nhận yêu cầu: thử model kế tiếp
                    _logger.LogWarning("Gemini model {Model} unavailable ({Reason}). Trying the next model if any.", model, ex.Message);
                    lastError = ex;
                }
            }
            throw lastError ?? new AiScoringException("AI tạm thời không phản hồi. Vui lòng thử lại sau.") { IsTransient = true };
        }
        #endregion

        #region Request
        /// <summary>
        /// Model chính trước, rồi các model dự phòng hợp lệ (bỏ trùng, bỏ model không phải văn bản).
        /// </summary>
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

        /// <summary>
        /// Gửi yêu cầu chấm với một model. 400 không do key (model không nhận temperature / chế độ JSON)
        /// -> gửi lại 1 lần với cấu hình tối giản.
        /// </summary>
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

            using (var retry = await SendAsync(http, model, systemPrompt, userPrompt, minimal: true, ct))
            {
                if (retry.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Gemini model {Model} succeeded with the minimal request (no temperature / JSON mode).", model);
                    return _parser.Parse(await retry.Content.ReadAsStringAsync(ct));
                }

                var reason = await ReadErrorReasonAsync(retry, ct);
                _logger.LogWarning("Gemini minimal request also failed: HTTP {Status} for model {Model}: {Reason}", (int)retry.StatusCode, model, reason);
                throw ToFriendlyException(retry.StatusCode, reason, model);
            }
        }

        /// <summary>
        /// Gửi một request generateContent. KHÔNG gắn key ở đây — GeminiKeyHandler gắn và xoay vòng.
        /// Nội dung dùng JsonContent (đọc lại được) vì handler có thể gửi lại request khi đổi key.
        /// </summary>
        private async Task<HttpResponseMessage> SendAsync(HttpClient http, string model, string systemPrompt, string userPrompt, bool minimal, CancellationToken ct)
        {
            object generationConfig = minimal
                ? new { maxOutputTokens = _options.MaxTokens }
                : new
                {
                    maxOutputTokens = _options.MaxTokens,
                    temperature = 0.2,                         // thấp = điểm ổn định hơn
                    responseMimeType = "application/json"      // yêu cầu Gemini trả JSON thuần
                };

            var body = new
            {
                systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
                contents = new[] { new { role = "user", parts = new[] { new { text = userPrompt } } } },
                generationConfig
            };

            // BaseAddress của "GeminiClient" đã có ".../v1beta/" -> chỉ ghép phần models/...
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

        /// <summary>
        /// Đổi mã lỗi HTTP thành thông báo tiếng Việt đúng bản chất (cấu hình hay quá tải).
        /// 429 ở đây nghĩa là handler đã thử hết các key mà vẫn bị giới hạn.
        /// </summary>
        private static AiScoringException ToFriendlyException(HttpStatusCode status, string reason, string model)
        {
            int code = (int)status;
            if (status == HttpStatusCode.NotFound)
                return new AiScoringException(
                    $"AI chưa được cấu hình đúng: không tìm thấy model '{model}' (mã 404). Quản trị viên cần kiểm tra Gemini:Model.",
                    isConfigurationError: true)
                { CanTryOtherModel = true };
            if (status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                || (status == HttpStatusCode.BadRequest && reason.Contains("API key", StringComparison.OrdinalIgnoreCase)))
                return new AiScoringException(
                    $"AI chưa được cấu hình đúng: API key không hợp lệ hoặc chưa được cấp quyền (mã {code}). Quản trị viên cần kiểm tra Gemini__ApiKeys.",
                    isConfigurationError: true);
            if (status == HttpStatusCode.BadRequest)
                return new AiScoringException(
                    $"Model AI '{model}' không nhận yêu cầu chấm điểm (mã 400). Quản trị viên xem log để biết lý do, hoặc đổi Gemini:Model.",
                    isConfigurationError: true)
                { CanTryOtherModel = true };
            if (status == HttpStatusCode.TooManyRequests)
                return new AiScoringException("AI đang quá tải hoặc đã hết lượt dùng miễn phí (mã 429). Vui lòng thử lại sau ít phút.") { IsTransient = true };
            if (code >= 500)
                return new AiScoringException($"AI tạm thời không phản hồi (mã {code}). Vui lòng thử lại sau.") { IsTransient = true };
            return new AiScoringException($"AI không xử lý được yêu cầu (mã {code}). Vui lòng thử lại.");
        }

        /// <summary>
        /// Đọc lý do lỗi từ body Gemini ({"error":{"status","message"}}, đôi khi bọc trong mảng).
        /// Không đọc được JSON thì trả nguyên văn (cắt ngắn). Body lỗi không chứa API key.
        /// </summary>
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
            catch (JsonException)
            {
                // không phải JSON: rơi xuống trả nguyên văn
            }
            return Truncate(body.ReplaceLineEndings(" "), 500);
        }

        private static string Truncate(string text, int max) => text.Length > max ? text[..max] + "..." : text;
        #endregion
    }
}