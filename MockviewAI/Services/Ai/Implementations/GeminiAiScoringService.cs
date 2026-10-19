// [AI-MODULE] GeminiAiScoringService
// Chỉ lo việc GỬI REQUEST tới Gemini (Google Generative Language API, generateContent) bằng HttpClient.
// Soạn prompt do IScoringPromptBuilder, đọc kết quả do IAiResponseParser. API key không bao giờ được log.

using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MockviewAI.Models.Ai;
using MockviewAI.Services.Ai.Interfaces;
using MockviewAI.Services.Exceptions;

namespace MockviewAI.Services.Ai.Implementations
{
    // Calls the Gemini generateContent REST API directly with HttpClient (no extra NuGet package).
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
                throw new AiScoringException("AI chưa được cấu hình (thiếu Gemini:ApiKey).");
            if (request.Answers.Count == 0)
                throw new AiScoringException("Chưa có câu trả lời nào để chấm.");

            // Model name comes from config only; reject anything that could change the URL path
            if (!System.Text.RegularExpressions.Regex.IsMatch(_options.Model ?? "", @"^[A-Za-z0-9._-]+$"))
                throw new AiScoringException("Tên model Gemini không hợp lệ (Gemini:Model).");

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
                    // Log only the status code, not the body (the body may echo user text)
                    _logger.LogWarning("Gemini returned HTTP {Status}", (int)response.StatusCode);
                    throw new AiScoringException("AI tạm thời không phản hồi. Vui lòng thử lại sau.");
                }

                var json = await response.Content.ReadAsStringAsync(ct);
                return _parser.Parse(json);
            }
        }
    }
}
