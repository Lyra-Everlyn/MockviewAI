using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using MockviewAI.Models.CvQuestions;

namespace MockviewAI.Services.CvQuestions.Implementations
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// Đọc JSON người kiểm duyệt trả về, chịu lỗi: JSON hỏng, thiếu index, verdict lạ -> reject.
    /// "pass" nhưng có tiêu chí false -> hạ xuống "fix" (không tin AI hoàn toàn).
    /// </summary>
    public static class CvReviewParser
    {
        #region Fields
        private const int MaxIssues = 5;
        private const int MaxIssueChars = 200;

        private static readonly Regex CodeFence = new(@"^```(?:json)?\s*|\s*```$", RegexOptions.Compiled);
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };
        #endregion

        #region Public
        /// <param name="count">Số câu đã gửi duyệt; kết quả luôn có đúng chừng này mục, theo thứ tự.</param>
        public static List<CvQuestionReview> Parse(string? json, int count)
        {
            List<ReviewItemDto> items;
            try
            {
                string text = CodeFence.Replace((json ?? string.Empty).Trim(), string.Empty);
                // Nhận cả mảng [...] lẫn { "reviews": [...] }
                var list = text.StartsWith('[')
                    ? JsonSerializer.Deserialize<List<ReviewItemDto>>(text, JsonOptions)
                    : JsonSerializer.Deserialize<ReviewResultDto>(text, JsonOptions)?.Reviews;
                items = list?.Where(r => r != null).ToList() ?? new List<ReviewItemDto>();
            }
            catch (JsonException)
            {
                items = new List<ReviewItemDto>();
            }

            var result = new List<CvQuestionReview>();
            for (int i = 0; i < count; i++)
            {
                var item = items.FirstOrDefault(r => r.Index == i);
                result.Add(item == null
                    ? new CvQuestionReview { Index = i, Verdict = CvQuestionReview.Reject, Issues = { "Người kiểm duyệt không trả kết quả cho câu này" } }
                    : ToReview(item, i));
            }
            return result;
        }
        #endregion

        #region Helpers
        private static CvQuestionReview ToReview(ReviewItemDto item, int index)
        {
            var issues = (item.Issues ?? new()).Select(s => (s ?? string.Empty).Trim()).Where(s => s.Length > 0)
                .Select(s => s.Length <= MaxIssueChars ? s : s[..MaxIssueChars]).Take(MaxIssues).ToList();
            string verdict = (item.Verdict ?? string.Empty).Trim().ToLowerInvariant();

            if (verdict is not (CvQuestionReview.Pass or CvQuestionReview.Fix or CvQuestionReview.Reject))
            {
                verdict = CvQuestionReview.Reject;
                issues.Add("Kết quả kiểm duyệt không hợp lệ");
            }

            // pass mà có tiêu chí false: tiêu chí bám CV / an toàn sai -> loại, tiêu chí khác -> sửa
            var failed = (item.Criteria ?? new()).Where(c => IsFalse(c.Value)).Select(c => c.Key.ToLowerInvariant()).ToList();
            if (verdict == CvQuestionReview.Pass && failed.Count > 0)
            {
                verdict = failed.Contains("grounded") || failed.Contains("safe") ? CvQuestionReview.Reject : CvQuestionReview.Fix;
                issues.Add("Chưa đạt tiêu chí: " + string.Join(", ", failed));
            }

            return new CvQuestionReview { Index = index, Verdict = verdict, Issues = issues };
        }

        /// <summary>false hoặc "false" (AI đôi khi trả chuỗi) = tiêu chí chưa đạt.</summary>
        private static bool IsFalse(JsonElement value) =>
            value.ValueKind == JsonValueKind.False
            || (value.ValueKind == JsonValueKind.String && string.Equals(value.GetString(), "false", StringComparison.OrdinalIgnoreCase));
        #endregion
    }
}
