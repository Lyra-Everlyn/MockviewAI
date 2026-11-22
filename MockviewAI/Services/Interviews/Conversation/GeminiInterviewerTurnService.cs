using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using MockviewAI.Services.Ai;
using MockviewAI.Services.KeyManagement.ProviderExtensions;

namespace MockviewAI.Services.Interviews.Conversation
{
    /// <summary>
    /// [INTERVIEWER] 
    /// Gọi Gemini (HttpClient "GeminiClient") để soạn phản hồi + câu hỏi đào sâu.
    /// Tắt, thiếu key, lỗi, quá giờ hoặc kết quả không hợp lệ: trả phản hồi có sẵn, câu hỏi thêm theo bộ đề.
    /// </summary>
    public class GeminiInterviewerTurnService : IInterviewerTurnService
    {
        #region Fields
        private const string HttpClientName = "GeminiClient";
        private const int MaxReactionChars = 300;
        private const int MaxFollowUpChars = 400;

        private static readonly Regex SafeModelName = new(@"^[A-Za-z0-9._-]+$", RegexOptions.Compiled);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GeminiKeyProvider _keys;
        private readonly InterviewerOptions _options;
        private readonly GeminiOptions _gemini;
        private readonly ILogger<GeminiInterviewerTurnService> _logger;
        #endregion

        #region Constructor
        public GeminiInterviewerTurnService(
            IHttpClientFactory httpClientFactory,
            GeminiKeyProvider keys,
            IOptions<InterviewerOptions> options,
            IOptions<GeminiOptions> gemini,
            ILogger<GeminiInterviewerTurnService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _keys = keys;
            _options = options.Value;
            _gemini = gemini.Value;
            _logger = logger;
        }
        #endregion

        #region IInterviewerTurnService
        public async Task<InterviewerTurn> NextTurnAsync(InterviewerTurnRequest request, CancellationToken ct = default)
        {
            var model = (string.IsNullOrWhiteSpace(_options.Model) ? _gemini.Model : _options.Model).Trim();
            if (!_options.Enabled || !_keys.HasKeys || !SafeModelName.IsMatch(model))
                return InterviewerFallback.For(request);

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 2, 20)));
            var watch = Stopwatch.StartNew();
            try
            {
                var json = await SendAsync(model, request, timeout.Token);
                var turn = Validate(Parse(json), request);
                _logger.LogInformation("[INTERVIEWER] {Model} replied in {Ms} ms (followUp={HasFollowUp}, cv={HasCv})",
                    model, watch.ElapsedMilliseconds, turn.FollowUp != null, request.CvContext.Length > 0);
                return turn;
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                _logger.LogWarning("[INTERVIEWER] {Model} failed after {Ms} ms ({Reason}), using fallback",
                    model, watch.ElapsedMilliseconds, ex.GetType().Name + ": " + ex.Message);
                return InterviewerFallback.For(request);
            }
        }
        #endregion

        #region Request
        /// <summary>Model từ chối thinkingConfig (HTTP 400) thì gửi lại một lần không kèm.</summary>
        private async Task<string> SendAsync(string model, InterviewerTurnRequest request, CancellationToken ct)
        {
            var thinking = (_options.ThinkingLevel ?? string.Empty).Trim();
            var client = _httpClientFactory.CreateClient(HttpClientName);

            using var response = await client.PostAsJsonAsync($"models/{model}:generateContent", BuildBody(request, thinking.Length > 0 ? thinking : null), ct);
            if ((int)response.StatusCode == 400 && thinking.Length > 0)
            {
                using var retry = await client.PostAsJsonAsync($"models/{model}:generateContent", BuildBody(request, null), ct);
                return await ReadOrThrowAsync(retry, ct);
            }
            return await ReadOrThrowAsync(response, ct);
        }

        private static async Task<string> ReadOrThrowAsync(HttpResponseMessage response, CancellationToken ct)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {(body.Length > 200 ? body[..200] : body)}");
            return body;
        }

        private static object BuildBody(InterviewerTurnRequest request, string? thinkingLevel)
        {
            var systemInstruction = new { parts = new[] { new { text = InterviewerPromptBuilder.SystemPrompt } } };
            var contents = new[] { new { role = "user", parts = new[] { new { text = InterviewerPromptBuilder.BuildUserPrompt(request) } } } };

            if (thinkingLevel is null)
                return new
                {
                    systemInstruction,
                    contents,
                    generationConfig = new { temperature = 0.6, maxOutputTokens = 1024, responseMimeType = "application/json" }
                };

            return new
            {
                systemInstruction,
                contents,
                generationConfig = new
                {
                    temperature = 0.6,
                    maxOutputTokens = 1024,
                    responseMimeType = "application/json",
                    thinkingConfig = new { thinkingLevel }
                }
            };
        }
        #endregion

        #region Response
        /// <summary>Ghép phần trả lời (bỏ phần "thought"), đọc reaction / followUp.</summary>
        private static (string Reaction, string? FollowUp) Parse(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var parts = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts");

            var sb = new StringBuilder();
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("thought", out var thought) && thought.ValueKind == JsonValueKind.True) continue;
                if (part.TryGetProperty("text", out var text)) sb.Append(text.GetString());
            }

            var raw = sb.ToString().Trim();
            int start = raw.IndexOf('{'), end = raw.LastIndexOf('}');
            if (start < 0 || end <= start) throw new JsonException("No JSON object in reply");

            using var turn = JsonDocument.Parse(raw[start..(end + 1)]);
            var root = turn.RootElement;
            string reaction = root.TryGetProperty("reaction", out var r) && r.ValueKind == JsonValueKind.String ? r.GetString()!.Trim() : string.Empty;
            string? followUp = root.TryGetProperty("followUp", out var f) && f.ValueKind == JsonValueKind.String ? f.GetString()!.Trim() : null;
            return (reaction, string.IsNullOrWhiteSpace(followUp) ? null : followUp);
        }

        /// <summary>
        /// Áp luật bằng code, không tin hoàn toàn vào AI: reaction có câu hỏi / quá dài -> câu có sẵn;
        /// không được hỏi thêm -> bỏ followUp; còn ý cốt lõi thiếu mà AI không hỏi -> hỏi theo bộ đề.
        /// </summary>
        private static InterviewerTurn Validate((string Reaction, string? FollowUp) reply, InterviewerTurnRequest request)
        {
            var reaction = reply.Reaction;
            if (reaction.Length == 0 || reaction.Length > MaxReactionChars || reaction.Contains('?'))
                reaction = InterviewerFallback.Acknowledge();

            string? followUp = null;
            if (request.AllowFollowUp)
            {
                followUp = reply.FollowUp is { Length: > 0 and <= MaxFollowUpChars } ? reply.FollowUp : null;
                followUp ??= request.MissingPointQuestion;
            }

            return new InterviewerTurn(reaction, followUp, FromAi: true);
        }
        #endregion
    }
}
