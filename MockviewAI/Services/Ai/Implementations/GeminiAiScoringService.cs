using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MockviewAI.Models.Ai;
using MockviewAI.Services.Ai.Interfaces;
using MockviewAI.Services.Exceptions;

namespace MockviewAI.Services.Ai.Implementations
{
    /// <summary>
    /// [AI-MODULE] 
    /// GeminiAiScoringService
    /// -- Chỉ lo việc GỬI REQUEST tới Gemini bằng HttpClient.
    /// -- Soạn prompt do IScoringPromptBuilder, đọc kết quả do IAiResponseParser. API key không bao giờ được log.
    /// -- Calls the Gemini generateContent REST API directly with HttpClient (no extra NuGet package).
    /// </summary>

    public class GeminiAiScoringService : IAiScoringService
    {
        private readonly HttpClient _http;
        private readonly GeminiOptions _options;
        private readonly IScoringPromptBuilder _promptBuilder;
        private readonly IAiResponseParser _parser;
        private readonly ILogger<GeminiAiScoringService> _logger;

        public GeminiAiScoringService(
            HttpClient http,
            IOptions<GeminiOptions> options,
            IScoringPromptBuilder promptBuilder,
            IAiResponseParser parser,
            ILogger<GeminiAiScoringService> logger)
        {
            _http = http;
            _options = options.Value;
            _promptBuilder = promptBuilder;
            _parser = parser;
            _logger = logger;
        }

        public async Task<ContentScoreResult> ScoreContentAsync(ScoreRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
                throw new AiScoringException("AI chưa được cấu hình (thiếu Gemini:ApiKey).", isConfigurationError: true);
            if (request.Answers.Count == 0)
                throw new AiScoringException("Chưa có câu trả lời nào để chấm.");

            // Model name comes from config only; reject anything that could change the URL path
            if (!System.Text.RegularExpressions.Regex.IsMatch(_options.Model ?? "", @"^[A-Za-z0-9._-]+$"))
                throw new AiScoringException("Tên model Gemini không hợp lệ (Gemini:Model).", isConfigurationError: true);

            // Chỉ model sinh VĂN BẢN mới chấm được. Model giọng nói/ảnh/video/embedding (vd gemini-3.8-flash-tts) trả về
            // âm thanh hoặc vector, không trả JSON -> báo lỗi cấu hình rõ ràng thay vì gọi API rồi nhận lỗi mơ hồ.
            if (IsNonTextModel(_options.Model!))
            {
                _logger.LogError("Gemini:Model '{Model}' is not a text-generation model. Use e.g. gemini-3.8-flash", _options.Model);
                throw new AiScoringException(
                    $"Model AI '{_options.Model}' không dùng để chấm văn bản. Quản trị viên cần đổi Gemini:Model (ví dụ gemini-3.8-flash).",
                    isConfigurationError: true);
            }

            var body = new
            {
                systemInstruction = new { parts = new[] { new { text = _promptBuilder.SystemPrompt } } },
                contents = new[] { new { role = "user", parts = new[] { new { text = _promptBuilder.BuildUserPrompt(request) } } } },
                generationConfig = new
                {
                    maxOutputTokens = _options.MaxTokens,
                    temperature = 0.2,                         // low = more consistent scores
                    responseMimeType = "application/json"      // ask Gemini for pure JSON
                }
            };

            using var msg = new HttpRequestMessage(HttpMethod.Post, $"v1beta/models/{_options.Model}:generateContent")
            {
                Content = JsonContent.Create(body)
            };
            msg.Headers.Add("x-goog-api-key", _options.ApiKey);     // never log this header

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(msg, ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Gemini request failed (network/timeout)");
                throw new AiScoringException("Không kết nối được tới AI. Vui lòng thử lại.", ex);
            }

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    // Ghi lý do Google trả về (vd "models/x is not found") để biết sửa gì; cắt ngắn, không có API key
                    var reason = await ReadErrorReasonAsync(response, ct);
                    _logger.LogWarning("Gemini returned HTTP {Status} for model {Model}: {Reason}",
                        (int)response.StatusCode, _options.Model, reason);
                    throw ToFriendlyException(response.StatusCode, reason);
                }

                var json = await response.Content.ReadAsStringAsync(ct);
                return _parser.Parse(json);
            }
        }

        private static readonly string[] NonTextModelMarkers =
            { "-tts", "-live", "native-audio", "transcribe", "image", "embedding", "imagen", "veo", "lyria", "omni", "robotics" };

        private static bool IsNonTextModel(string model) =>
            NonTextModelMarkers.Any(m => model.Contains(m, StringComparison.OrdinalIgnoreCase));

        // Đổi mã lỗi HTTP thành thông báo tiếng Việt đúng bản chất (cấu hình hay quá tải)
        private AiScoringException ToFriendlyException(HttpStatusCode status, string reason)
        {
            int code = (int)status;
            if (status == HttpStatusCode.NotFound)
                return new AiScoringException(
                    $"AI chưa được cấu hình đúng: không tìm thấy model '{_options.Model}' (mã 404). Quản trị viên cần kiểm tra Gemini:Model (ví dụ gemini-3.8-flash).",
                    isConfigurationError: true);
            if (status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                || (status == HttpStatusCode.BadRequest && reason.Contains("API key", StringComparison.OrdinalIgnoreCase)))
                return new AiScoringException(
                    $"AI chưa được cấu hình đúng: API key không hợp lệ hoặc chưa được cấp quyền (mã {code}). Quản trị viên cần kiểm tra Gemini:ApiKey.",
                    isConfigurationError: true);
            if (status == HttpStatusCode.TooManyRequests)
                return new AiScoringException("AI đang quá tải hoặc đã hết lượt dùng miễn phí (mã 429). Vui lòng thử lại sau ít phút.");
            if (code >= 500)
                return new AiScoringException($"AI tạm thời không phản hồi (mã {code}). Vui lòng thử lại sau.");
            return new AiScoringException($"AI không xử lý được yêu cầu (mã {code}). Vui lòng thử lại.");
        }

        // Gemini trả lỗi dạng {"error":{"code":404,"message":"...","status":"NOT_FOUND"}}: chỉ lấy status + message
        private static async Task<string> ReadErrorReasonAsync(HttpResponseMessage response, CancellationToken ct)
        {
            try
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(body);
                var error = doc.RootElement.GetProperty("error");
                var status = error.TryGetProperty("status", out var s) ? s.GetString() : "";
                var message = error.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "";
                var text = $"{status} {message}".Trim();
                return text.Length > 300 ? text[..300] + "..." : text;
            }
            catch (Exception)
            {
                return "(không đọc được nội dung lỗi)";
            }
        }
    }
}
