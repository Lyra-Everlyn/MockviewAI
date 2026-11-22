using MockviewAI.Models.Ai;
using MockviewAI.Models.InterviewQuestions;
using MockviewAI.Services.InterviewQuestions.Interfaces;

namespace MockviewAI.Services.InterviewQuestions.Implementations
{
    /// <summary>
    /// [FOLLOW-UP] 
    /// Chọn câu hỏi thêm theo ý cốt lõi còn thiếu, không phụ thuộc AI.
    /// Luật: chấm sơ bộ theo bộ đề ra "Sai" -> hỏi thêm về ý cốt lõi ĐẦU TIÊN chưa đạt (theo thứ tự c1, c2...).
    /// Câu trả lời đã tới điểm B thì không hỏi thêm.
    /// </summary>
    /// <remarks>
    /// Đo trên câu mẫu chưa dùng để chỉnh từ khoá: hỏi thêm 55/59 câu Sai, hỏi thừa 2/177 câu Đúng / Hơi đặc biệt / Đặc biệt.
    /// Giới hạn: từ khoá không hiểu nghĩa nên có thể hỏi thêm về ý người dùng đã nói bằng cách diễn đạt khác
    /// (câu hỏi thêm được soạn mềm kiểu "bạn nói rõ hơn..." nên vẫn tự nhiên).
    /// </remarks>
    public class FollowUpPlanner : IFollowUpPlanner
    {
        #region Fields
        private readonly IOfflineAnswerScorer _scorer;
        #endregion

        #region Constructor
        public FollowUpPlanner(IOfflineAnswerScorer scorer)
        {
            _scorer = scorer;
        }
        #endregion

        #region IFollowUpPlanner
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
        #endregion
    }
}
