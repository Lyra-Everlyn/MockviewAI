// [QUESTION-BANK] Chấm SƠ BỘ theo bộ đề bằng từ khoá - giúp hệ thống KHÔNG phụ thuộc hoàn toàn vào AI.
// Thứ tự xét (giống thứ tự AI được dặn):
//   1. Có dấu hiệu một LỖI HAY GẶP (không bị phủ định bởi "không / tránh / never...") -> sai.
//   2. Đếm ý cốt lõi đạt: ý đạt khi gặp >= MinHits từ khoá khác nhau của ý đó.
//   3. Đạt ít hơn MinCorePoints ý -> sai (chưa tới điểm B).
//   4. Có dấu hiệu một ĐƯỜNG VÒNG (cùng công thức nhưng cách khác) -> hoi_dac_biet; còn lại -> dung.
// Giới hạn (ghi rõ trong báo cáo): không nhận ra được "dac_biet" (hướng đi hoàn toàn mới) và "hơi đặc biệt do cách
// giải thích khác thường" - hai loại này cần AI hiểu nghĩa. Đo trên 140 câu mẫu + 16 câu mới: 0 câu Sai bị chấm thành đúng.

using MockviewAI.Models.Ai;
using MockviewAI.Models.InterviewQuestions;
using MockviewAI.Services.InterviewQuestions.Interfaces;

namespace MockviewAI.Services.InterviewQuestions.Implementations
{
    public class BankKeywordScorer : IOfflineAnswerScorer
    {
        // Từ phủ định (đã bỏ dấu). KHÔNG có "dung" vì "dùng" và "đừng" bỏ dấu đều thành "dung".
        private static readonly HashSet<string> Negators = new() { "khong", "tranh", "chang", "cam", "chua", "not", "never", "dont" };
        private const int NegationWindow = 3;   // xét 3 từ đứng trước

        public const string FeedbackPrefix = "Chấm sơ bộ theo bộ đề (không dùng AI). ";

        public AnswerVerdict Score(BankQuestion question, string answer)
        {
            var tokens = VietnameseTextNormalizer.Tokenize(answer);
            var coreIds = question.CorePoints.Select(c => c.Id).ToList();

            // 1. Lỗi hay gặp
            var mistake = question.Mistakes.FirstOrDefault(m => m.Signals.Any(s => Contains(tokens, s, checkNegation: true)));
            if (mistake != null)
            {
                return new AnswerVerdict
                {
                    QuestionId = question.Id,
                    Category = AnswerCategory.Wrong,
                    MatchedPath = mistake.Id,
                    MissingCorePoints = coreIds,
                    Feedback = FeedbackPrefix + "Câu trả lời có dấu hiệu lỗi hay gặp: " + mistake.Summary + "."
                };
            }

            // 2. Ý cốt lõi đã đạt
            var met = MetCorePoints(question, tokens);

            // 4 (tính trước). Con đường: ưu tiên đường vòng nếu có dấu hiệu, không thì đường có nhiều dấu hiệu nhất
            AnswerPath? path = null;
            int bestHits = 0;
            foreach (var p in question.Paths)
            {
                int hits = p.Signals.Count(s => Contains(tokens, s, checkNegation: false));
                if (hits > 0 && p.Category == AnswerCategory.AlternativePath) { path = p; break; }
                if (hits > bestHits) { path = p; bestHits = hits; }
            }

            // 3. Chưa tới điểm B
            string category = met.Count < question.MinCorePoints ? AnswerCategory.Wrong
                            : path?.Category == AnswerCategory.AlternativePath ? AnswerCategory.AlternativePath
                            : AnswerCategory.Correct;

            string feedback = category == AnswerCategory.Wrong
                ? $"Chưa nhận ra đủ ý cốt lõi (đạt {met.Count}/{coreIds.Count}, cần ít nhất {question.MinCorePoints}). Hãy nói rõ hơn các ý còn thiếu."
                : $"Đã chạm {met.Count}/{coreIds.Count} ý cốt lõi.";

            return new AnswerVerdict
            {
                QuestionId = question.Id,
                Category = category,
                MatchedPath = path?.Id ?? AnswerCategory.OutOfBank,
                MetCorePoints = met,
                MissingCorePoints = coreIds.Where(id => !met.Contains(id)).ToList(),
                Feedback = FeedbackPrefix + feedback
            };
        }

        public IReadOnlyList<string> FindMetCorePoints(BankQuestion question, string answer) =>
            MetCorePoints(question, VietnameseTextNormalizer.Tokenize(answer));

        // Ý đạt khi gặp >= MinHits từ khoá khác nhau của ý đó
        private static List<string> MetCorePoints(BankQuestion question, string[] tokens) =>
            question.CorePoints
                .Where(c => c.Keywords.Distinct().Count(k => Contains(tokens, k, checkNegation: false)) >= Math.Max(1, c.MinHits))
                .Select(c => c.Id)
                .ToList();

        // Cụm từ (đã chuẩn hoá) có xuất hiện liền nhau trong câu trả lời không; có thể bỏ qua lần xuất hiện bị phủ định
        private static bool Contains(string[] tokens, string phrase, bool checkNegation)
        {
            var words = VietnameseTextNormalizer.Tokenize(phrase);
            if (words.Length == 0) return false;
            for (int i = 0; i + words.Length <= tokens.Length; i++)
            {
                bool same = true;
                for (int j = 0; j < words.Length && same; j++) same = tokens[i + j] == words[j];
                if (!same) continue;
                if (checkNegation && IsNegated(tokens, i)) continue;
                return true;
            }
            return false;
        }

        private static bool IsNegated(string[] tokens, int start)
        {
            for (int k = Math.Max(0, start - NegationWindow); k < start; k++)
                if (Negators.Contains(tokens[k])) return true;
            return false;
        }
    }
}
