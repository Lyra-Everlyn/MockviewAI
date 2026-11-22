using MockviewAI.Models.Ai;
using MockviewAI.Models.Entities;

namespace MockviewAI.Services.Interviews.Results
{
    /// <summary>
    /// [AI-SCORING] 
    /// Dựng yêu cầu chấm điểm từ một buổi luyện đã kết thúc.
    /// </summary>
    public interface IScoreRequestFactory
    {
        /// <summary>
        /// Chỉ câu CHÍNH được chấm; câu trả lời thêm có ý nghĩa gộp vào câu chính của nó.
        /// Trả về null khi không có câu chính nào được trả lời (buổi sẽ bị đánh dấu Dừng).
        /// </summary>
        ScoreRequest? Build(InterviewSession session);
    }
}
