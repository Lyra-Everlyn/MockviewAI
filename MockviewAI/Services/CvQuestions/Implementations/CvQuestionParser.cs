using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using MockviewAI.Models.CvQuestions;
using MockviewAI.Models.InterviewQuestions;
using MockviewAI.Services.InterviewQuestions;

namespace MockviewAI.Services.CvQuestions.Implementations
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// Đọc JSON người soạn trả về thành BankQuestion. Không tin AI: cắt độ dài, bỏ mục rỗng, tự đặt mã ý / lỗi nếu thiếu.
    /// Id, Interviewer, Positions do code gán; Samples để rỗng. JSON hỏng thì trả danh sách rỗng.
    /// </summary>
    public static class CvQuestionParser
    {
        #region Fields
        private const int MaxTextChars = 300;
        private const int MaxKeywordChars = 40;
        private const int MaxListItems = 10;

        private static readonly Regex CodeFence = new(@"^```(?:json)?\s*|\s*```$", RegexOptions.Compiled);

        /// <summary>Đầu dòng là tên trường thiếu ngoặc kép mở: <c>  minCorePoints": 2</c>.</summary>
        private static readonly Regex MissingOpenQuote = new(@"(?m)^(\s*)([A-Za-z_][A-Za-z0-9_]*)""(\s*:)", RegexOptions.Compiled);
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };
        #endregion

        #region Public
        /// <param name="json">Phần chữ AI trả về (có thể kèm khung ```json).</param>
        public static List<CvQuestionDraft> Parse(string? json, string positionKey, string interviewerKey) =>
            Parse(json, positionKey, interviewerKey, out _);

        /// <param name="problem">Lý do đọc thiếu câu (chỉ vị trí / loại lỗi, không chứa nội dung) để ghi log; null = đọc hết.</param>
        public static List<CvQuestionDraft> Parse(string? json, string positionKey, string interviewerKey, out string? problem)
        {
            problem = null;
            var result = new List<CvQuestionDraft>();
            string text = CodeFence.Replace((json ?? string.Empty).Trim(), string.Empty);
            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(text);
            }
            catch (JsonException)
            {
                // Lỗi hay gặp của bản lite: thiếu ngoặc kép mở ở tên trường (minCorePoints": 2) -> vá rồi đọc lại
                try
                {
                    doc = JsonDocument.Parse(MissingOpenQuote.Replace(text, "$1\"$2\"$3"));
                }
                catch (JsonException ex)
                {
                    problem = $"JSON hỏng (dòng {ex.LineNumber}, vị trí {ex.BytePositionInLine})";
                    return result;
                }
            }

            using (doc)
            {
                // Nhận { "questions": [...] }, mảng trần [...] (Gemini đôi khi trả, đo 1/5 lần) hoặc một câu đơn lẻ
                var root = doc.RootElement;
                IEnumerable<JsonElement> items = root.ValueKind switch
                {
                    JsonValueKind.Array => root.EnumerateArray(),
                    JsonValueKind.Object when TryGetQuestions(root, out var list) => list.EnumerateArray(),
                    JsonValueKind.Object when root.TryGetProperty("question", out _) => new[] { root },
                    _ => Array.Empty<JsonElement>()
                };

                int bad = 0;
                foreach (var element in items)
                {
                    // Đọc TỪNG câu: một câu sai kiểu (vd keywords là chuỗi) chỉ loại câu đó, không làm mất cả lượt soạn
                    try
                    {
                        var item = element.Deserialize<GeneratedQuestionDto>(JsonOptions);
                        if (item != null) result.Add(ToDraft(item, positionKey, interviewerKey, result.Count + 1));
                    }
                    catch (JsonException)
                    {
                        bad++;
                    }
                }
                if (bad > 0) problem = $"{bad} câu sai kiểu dữ liệu";
                else if (result.Count == 0) problem = $"không thấy danh sách câu hỏi (gốc là {root.ValueKind})";
            }
            return result;
        }
        #endregion

        #region Helpers
        /// <summary>Khoá "questions" (không phân biệt hoa thường) chứa mảng.</summary>
        private static bool TryGetQuestions(JsonElement root, out JsonElement list)
        {
            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Name.Equals("questions", StringComparison.OrdinalIgnoreCase) && prop.Value.ValueKind == JsonValueKind.Array)
                {
                    list = prop.Value;
                    return true;
                }
            }
            list = default;
            return false;
        }

        private static CvQuestionDraft ToDraft(GeneratedQuestionDto item, string positionKey, string interviewerKey, int n)
        {
            var corePoints = (item.CorePoints ?? new()).Where(c => c != null).Select((c, i) => new CorePoint
            {
                Id = IdOr(c.Id, "c" + (i + 1)),
                Text = Clip(c.Text, MaxTextChars),
                Keywords = Words(c.Keywords),
                MinHits = Math.Clamp(c.MinHits, 1, 2),
                FollowUp = string.IsNullOrWhiteSpace(c.FollowUp) ? null : Clip(c.FollowUp, MaxTextChars)
            }).ToList();

            var paths = (item.Paths ?? new()).Where(p => p != null).Select((p, i) => new AnswerPath
            {
                Id = IdOr(p.Id, i == 0 ? "chuan" : "vong" + i),
                Category = (p.Category ?? string.Empty).Trim().ToLowerInvariant(),
                Summary = Clip(p.Summary, MaxTextChars),
                Signals = Words(p.Signals),
                CoversCorePoints = (p.CoversCorePoints ?? new()).Select(id => (id ?? string.Empty).Trim()).Where(id => id.Length > 0).Distinct().ToList()
            }).ToList();

            var mistakes = (item.Mistakes ?? new()).Where(m => m != null).Select((m, i) => new CommonMistake
            {
                Id = IdOr(m.Id, "loi" + (i + 1)),
                Summary = Clip(m.Summary, MaxTextChars),
                Why = Clip(m.Why, MaxTextChars),
                Signals = Words(m.Signals)
            }).Where(m => m.Summary.Length > 0).Take(3).ToList();

            return new CvQuestionDraft
            {
                CvEvidence = Clip(item.CvEvidence, MaxTextChars),
                Question = new BankQuestion
                {
                    Id = $"{QuestionBankStoreExtensions.CvIdPrefix}new-{n}",   // mã tạm, gán mã thật khi lưu buổi
                    Interviewer = interviewerKey,
                    Positions = new List<string> { positionKey },
                    Skills = Words(item.Skills),
                    Level = Clip(item.Level, 20),
                    // Câu hỏi giữ nguyên độ dài để luật code loại câu quá 250 ký tự (không cắt ngầm)
                    Question = (item.Question ?? string.Empty).Trim(),
                    MinCorePoints = item.MinCorePoints >= 1 ? item.MinCorePoints : Math.Min(2, corePoints.Count),
                    CorePoints = corePoints,
                    Paths = paths,
                    Mistakes = mistakes,
                    Samples = new List<SampleAnswer>()
                }
            };
        }

        /// <summary>Danh sách chữ ngắn: bỏ rỗng, trùng, cắt độ dài, tối đa MaxListItems mục.</summary>
        private static List<string> Words(List<string>? items) =>
            (items ?? new()).Select(w => Clip(w, MaxKeywordChars)).Where(w => w.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase).Take(MaxListItems).ToList();

        /// <summary>Mã ngắn gọn (chữ, số, _ -), sai thì dùng mã mặc định.</summary>
        private static string IdOr(string? id, string fallback)
        {
            var v = (id ?? string.Empty).Trim();
            return v.Length is > 0 and <= 20 && v.All(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-') ? v : fallback;
        }

        private static string Clip(string? text, int max)
        {
            var t = (text ?? string.Empty).Trim();
            return t.Length <= max ? t : t[..max];
        }
        #endregion
    }
}
