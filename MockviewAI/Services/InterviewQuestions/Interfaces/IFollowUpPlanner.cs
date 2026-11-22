using MockviewAI.Models.InterviewQuestions;

namespace MockviewAI.Services.InterviewQuestions.Interfaces
{
    /// <summary>
    /// [FOLLOW-UP] 
    /// Quyết định có hỏi thêm sau một câu trả lời không, và hỏi về ý nào (không gọi AI).
    /// </summary>
    public interface IFollowUpPlanner
    {
        /// <returns>null = không cần hỏi thêm (đã tới điểm B, hoặc ý còn thiếu chưa có câu hỏi thêm soạn sẵn).</returns>
        FollowUpPlan? Plan(BankQuestion question, string answer);
    }

    /// <summary>[FOLLOW-UP] Ý cốt lõi sẽ hỏi thêm và câu hỏi soạn sẵn của ý đó.</summary>
    /// <param name="PointId">Mã ý: c1, c2...</param>
    /// <param name="Question">Câu hỏi thêm trong bộ đề (nhà tuyển dụng AI có thể diễn đạt lại).</param>
    public record FollowUpPlan(string PointId, string Question);
}
