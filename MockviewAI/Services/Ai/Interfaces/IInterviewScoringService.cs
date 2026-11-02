// [AI-MODULE] IInterviewScoringService
// Điểm vào của module AI: nhận ScoreRequest, trả InterviewScoreResult (nội dung + cách nói + tổng).

using MockviewAI.Models.Ai;

namespace MockviewAI.Services.Ai.Interfaces
{
    public interface IInterviewScoringService
    {
        Task<InterviewScoreResult> ScoreAsync(ScoreRequest request, CancellationToken ct = default);

        // Chấm KHÔNG CẦN AI khi AI lỗi / quá tải:
        //  - câu thuộc bộ đề -> chấm SƠ BỘ theo bộ đề (ý cốt lõi, lỗi hay gặp) + cách nói, có nhãn 4 loại;
        //  - câu hỏi kiểu cũ (không có bộ đề) -> chỉ tính cách nói như trước.
        InterviewScoreResult ScoreWithoutAi(ScoreRequest request);
    }
}
