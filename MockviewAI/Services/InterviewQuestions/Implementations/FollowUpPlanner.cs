// [FOLLOW-UP] Đợt 8: chọn câu hỏi thêm theo ý cốt lõi còn thiếu (cách A, không phụ thuộc AI).
// Luật: chấm sơ bộ theo bộ đề ra "Sai" (thiếu ý cốt lõi hoặc có dấu hiệu lỗi hay gặp) -> hỏi thêm về ý cốt lõi
// ĐẦU TIÊN chưa đạt (theo thứ tự trong bộ đề: c1, c2, ...). Câu trả lời đã tới điểm B thì không hỏi thêm.
// Đo trên câu mẫu chưa dùng để chỉnh từ khoá (03/11): hỏi thêm 55/59 câu Sai, hỏi thừa 2/177 câu đúng / hơi đặc biệt / đặc biệt.
// Giới hạn: từ khoá không hiểu nghĩa -> có thể hỏi thêm về ý người dùng đã nói bằng cách diễn đạt khác
// (câu hỏi thêm được soạn mềm: "bạn nói rõ hơn..." nên vẫn tự nhiên).

using MockviewAI.Models.Ai;
using MockviewAI.Models.InterviewQuestions;
using MockviewAI.Services.InterviewQuestions.Interfaces;

namespace MockviewAI.Services.InterviewQuestions.Implementations
{
    public class FollowUpPlanner : IFollowUpPlanner
    {
        private readonly IOfflineAnswerScorer _scorer;

        public FollowUpPlanner(IOfflineAnswerScorer scorer)
        {
            _scorer = scorer;
        }

        public FollowUpPlan? Plan(BankQuestion question, string answer)
        {
            if (question == null || string.IsNullOrWhiteSpace(answer)) return null;

            var verdict = _scorer.Score(question, answer);
            if (verdict.Category != AnswerCategory.Wrong) return null;

            // Khi có dấu hiệu lỗi hay gặp, verdict coi mọi ý là thiếu -> tính lại ý đã đạt để không hỏi lại ý đã nói
            var met = _scorer.FindMetCorePoints(question, answer);
            var point = question.CorePoints.FirstOrDefault(c => !met.Contains(c.Id) && !string.IsNullOrWhiteSpace(c.FollowUp));
            return point == null ? null : new FollowUpPlan(point.Id, point.FollowUp!.Trim());
        }
    }
}
