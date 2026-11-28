using System.Text.Json;
using System.Text.RegularExpressions;

namespace AiLoadTest
{
    /// <summary>
    /// Handler đặt SAU handler gắn khoá: thấy từng lần gửi thật. Chỉ đọc tên model (đường dẫn Gemini / trường "model" của Groq),
    /// không đọc, không ghi khoá hay nội dung.
    /// </summary>
    public class HttpStatusCounter : DelegatingHandler
    {
        private static readonly Regex GeminiModel = new(@"models/([^:/]+):", RegexOptions.Compiled);
        private static readonly Regex MultipartModel = new("name=model\\r\\n\\r\\n([^\\r\\n]+)", RegexOptions.Compiled);
        private readonly HttpStatusTally _tally;

        public HttpStatusCounter(HttpStatusTally tally)
        {
            _tally = tally;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            string provider = request.RequestUri?.Host.Contains("groq", StringComparison.OrdinalIgnoreCase) == true ? "Groq" : "Gemini";
            string model = await ModelOfAsync(request, provider, ct);
            try
            {
                var response = await base.SendAsync(request, ct);
                _tally.Add(provider, model, (int)response.StatusCode);
                return response;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _tally.Add(provider, model, 0);   // 0 = mất mạng / quá giờ
                throw;
            }
        }

        private static async Task<string> ModelOfAsync(HttpRequestMessage request, string provider, CancellationToken ct)
        {
            if (provider == "Gemini")
                return GeminiModel.Match(request.RequestUri?.AbsolutePath ?? "") is { Success: true } m ? m.Groups[1].Value : "?";
            if (request.Content == null) return "?";
            var body = await request.Content.ReadAsStringAsync(ct);
            if (MultipartModel.Match(body) is { Success: true } mm) return mm.Groups[1].Value;
            try
            {
                using var doc = JsonDocument.Parse(body);
                return doc.RootElement.TryGetProperty("model", out var p) ? p.GetString() ?? "?" : "?";
            }
            catch (JsonException) { return "?"; }
        }
    }
}
