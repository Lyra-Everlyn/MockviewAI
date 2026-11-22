using System.Text.Json;
using Microsoft.Extensions.Logging;
using MockviewAI.Models.Ai;
using MockviewAI.Services.Ai.Interfaces;
using MockviewAI.Services.Exceptions;

namespace MockviewAI.Services.Ai.Implementations
{
    /// <summary>
    /// [AI-SCORING] 
    /// Đọc phản hồi chấm điểm của Gemini: lấy JSON, ép mỗi điểm về 0..25 (không tin model),
    /// đọc thô phần xếp loại từng câu (AnswerVerdictValidator kiểm tra lại sau).
    /// </summary>
    public class GeminiResponseParser : IAiResponseParser
    {
        #region Fields
        /// <summary>Tối đa 10 câu / buổi.</summary>
        private const int MaxVerdicts = 10;

        private const int MaxIds = 10;

        /// <summary>Số điểm mạnh / điều cần cải thiện tối đa giữ lại.</summary>
        private const int MaxListItems = 3;

        private readonly ILogger<GeminiResponseParser> _logger;
        #endregion

        #region Constructor
        public GeminiResponseParser(ILogger<GeminiResponseParser> logger)
        {
            _logger = logger;
        }
        #endregion

        #region IAiResponseParser
        public ContentScoreResult Parse(string apiResponseJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(apiResponseJson);
                // candidates[0].content.parts[*].text (không có khi prompt bị chặn)
                var parts = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts");
                var text = string.Concat(parts.EnumerateArray()
                    .Select(x => x.TryGetProperty("text", out var t) ? t.GetString() : null));

                // Gemini lẽ ra trả JSON thuần, nhưng vẫn chịu được khung ```json hoặc chữ thừa
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
                    // [QUESTION-BANK] Phần xếp loại thiếu / hỏng thì để trống, KHÔNG làm mất điểm 4 tiêu chí
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
        #endregion

        #region Helpers
        /// <summary>Không tin model: ép mọi điểm về 0..25.</summary>
        private static int Clamp25(JsonElement e, string name) =>
            e.TryGetProperty(name, out var v) && v.TryGetDouble(out var d)
                ? (int)Math.Clamp(Math.Round(d), 0, 25) : 0;

        private static string Str(JsonElement e, string name) =>
            e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? (v.GetString() ?? "") : "";

        /// <summary>Đọc "answers":[{questionId, category, matchedPath, metCorePoints, missingCorePoints, feedback}].</summary>
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
                foreach (var item in v.EnumerateArray().Take(MaxListItems))
                    if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                        list.Add(item.GetString()!);
            return list;
        }
        #endregion
    }
}
