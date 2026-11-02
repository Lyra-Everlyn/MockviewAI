// [AI-MODULE] GeminiResponseParser
// Chỉ lo việc ĐỌC phản hồi của Gemini: lấy JSON điểm, ép mỗi điểm về 0..25 (không tin model).

using System.Text.Json;
using Microsoft.Extensions.Logging;
using MockviewAI.Models.Ai;
using MockviewAI.Services.Ai.Interfaces;
using MockviewAI.Services.Exceptions;

namespace MockviewAI.Services.Ai.Implementations
{
    public class GeminiResponseParser : IAiResponseParser
    {
        private const int MaxVerdicts = 10;   // tối đa 10 câu / buổi
        private const int MaxIds = 10;

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
                    Improvements = StrList(r, "improvements"),
                    // [QUESTION-BANK] Phần xếp loại từng câu: thiếu / hỏng thì để trống, KHÔNG làm mất điểm 4 tiêu chí
                    Verdicts = Verdicts(r)
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

        // Đọc "answers":[{questionId, category, matchedPath, metCorePoints, missingCorePoints, feedback}].
        // Chỉ đọc thô ở đây; AnswerVerdictValidator kiểm tra lại theo bộ đề.
        private static List<AnswerVerdict> Verdicts(JsonElement e)
        {
            var list = new List<AnswerVerdict>();
            if (!e.TryGetProperty("answers", out var arr) || arr.ValueKind != JsonValueKind.Array) return list;
            foreach (var item in arr.EnumerateArray().Take(MaxVerdicts))
            {
                if (item.ValueKind != JsonValueKind.Object) continue;
                list.Add(new AnswerVerdict
                {
                    QuestionId = Str(item, "questionId"),
                    Category = Str(item, "category"),
                    MatchedPath = Str(item, "matchedPath"),
                    MetCorePoints = Ids(item, "metCorePoints"),
                    MissingCorePoints = Ids(item, "missingCorePoints"),
                    Feedback = Str(item, "feedback")
                });
            }
            return list;
        }

        private static List<string> Ids(JsonElement e, string name)
        {
            var list = new List<string>();
            if (e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Array)
                foreach (var item in v.EnumerateArray().Take(MaxIds))
                    if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                        list.Add(item.GetString()!.Trim());
            return list;
        }

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
