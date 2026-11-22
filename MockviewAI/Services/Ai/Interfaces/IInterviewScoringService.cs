using MockviewAI.Models.Ai;

namespace MockviewAI.Services.Ai.Interfaces
{
    /// <summary>
    /// [AI-SCORING] 
    /// Điểm vào của module chấm: nhận ScoreRequest, trả điểm nội dung + cách nói + tổng.
    /// </summary>
    public interface IInterviewScoringService
    {
        /// <summary>Chấm đầy đủ: AI chấm nội dung, code chấm cách nói.</summary>
        /// <exception cref="Exceptions.AiScoringException">AI lỗi; buổi giữ nguyên để người dùng chấm lại hoặc chọn chấm sơ bộ.</exception>
        Task<InterviewScoreResult> ScoreAsync(ScoreRequest request, CancellationToken ct = default);

        /// <summary>
        /// Chấm không cần AI (khi AI lỗi / quá tải): câu thuộc bộ đề chấm sơ bộ theo ý cốt lõi + lỗi hay gặp, có nhãn 4 loại;
        /// câu hỏi kiểu cũ chỉ tính cách nói.
        /// </summary>
        InterviewScoreResult ScoreWithoutAi(ScoreRequest request);
    }
}
