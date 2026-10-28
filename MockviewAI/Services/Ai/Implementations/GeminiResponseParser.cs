using System.Text.Json;
using Microsoft.Extensions.Logging;
using MockviewAI.Models.Ai;
using MockviewAI.Services.Ai.Interfaces;
using MockviewAI.Services.Exceptions;

namespace MockviewAI.Services.Ai.Implementations
{
    /// <summary>
    /// [AI-MODULE] 
    /// GeminiResponseParser
    /// -- Chỉ lo việc ĐỌC phản hồi của Gemini: lấy JSON điểm, ép mỗi điểm về 0..25 (không tin model).
    /// </summary>

    public class GeminiResponseParser : IAiResponseParser
    {
        private readonly ILogger<GeminiResponseParser> _logger;

        public GeminiResponseParser(ILogger<GeminiResponseParser> logger)
        {
            _logger = logger;
        }

        public ContentScoreResult Parse(string apiResponseJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(apiResponseJson);
                // Gemini reply: candidates[0].content.parts[*].text (missing when the prompt was blocked)
                var parts = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts");
                var text = string.Concat(parts.EnumerateArray()
                    .Select(x => x.TryGetProperty("text", out var t) ? t.GetString() : null));

                // Gemini should return pure JSON, but tolerate ```json fences or extra words
                int start = text.IndexOf('{'), end = text.LastIndexOf('}');
                if (start < 0 || end <= start) throw new FormatException("No JSON object in AI reply");

                using var scoreDoc = JsonDocument.Parse(text[start..(end + 1)]);
                var r = scoreDoc.RootElement;

                return new ContentScoreResult
                {
                    Relevance = Clamp25(r, "relevance"),
                    Structure = Clamp25(r, "structure"),
                    Specificity = Clamp25(r, "specificity"),
                    Clarity = Clamp25(r, "clarity"),
                    Summary = Str(r, "summary"),
                    Strengths = StrList(r, "strengths"),
                    Improvements = StrList(r, "improvements")
                };
            }
            // IndexOutOfRangeException: Gemini trả "candidates": [] (bị chặn / rỗng) -> báo lỗi thân thiện thay vì lỗi 500
            catch (Exception ex) when (ex is JsonException or KeyNotFoundException or FormatException or InvalidOperationException or IndexOutOfRangeException)
            {
                _logger.LogWarning(ex, "Could not parse Gemini reply");
                throw new AiScoringException("AI trả về kết quả không đọc được. Vui lòng thử lại.", ex);
            }
        }

        // Never trust the model: force every score into 0..25
        private static int Clamp25(JsonElement e, string name) =>
            e.TryGetProperty(name, out var v) && v.TryGetDouble(out var d)
                ? (int)Math.Clamp(Math.Round(d), 0, 25) : 0;

        private static string Str(JsonElement e, string name) =>
            e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? (v.GetString() ?? "") : "";

        private static List<string> StrList(JsonElement e, string name)
        {
            var list = new List<string>();
            if (e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Array)
                foreach (var item in v.EnumerateArray().Take(3))
                    if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                        list.Add(item.GetString()!);
            return list;
        }
    }
}
