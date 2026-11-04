// [FOLLOW-UP] Đợt 8: quyết định có hỏi thêm sau một câu trả lời không, và hỏi thêm câu gì (không gọi AI).

using MockviewAI.Models.InterviewQuestions;

namespace MockviewAI.Services.InterviewQuestions.Interfaces
{
    public interface IFollowUpPlanner
    {
        // null = không cần hỏi thêm (đã tới điểm B, hoặc ý còn thiếu chưa có câu hỏi thêm soạn sẵn)
        FollowUpPlan? Plan(BankQuestion question, string answer);
    }

    // Ý cốt lõi sẽ hỏi thêm (c2...) và câu hỏi thêm soạn sẵn của ý đó
    public record FollowUpPlan(string PointId, string Question);
}
