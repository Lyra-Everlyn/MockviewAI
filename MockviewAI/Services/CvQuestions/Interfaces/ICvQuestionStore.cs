using MockviewAI.Models.CvQuestions;
using MockviewAI.Models.Entities;
using MockviewAI.Models.InterviewQuestions;

namespace MockviewAI.Services.CvQuestions.Interfaces
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// Lưu / đọc câu hỏi AI soạn theo CV (bảng CvGeneratedQuestions). Mọi truy vấn lọc theo chủ buổi (UserId).
    /// </summary>
    public interface ICvQuestionStore
    {
        /// <summary>Câu CV của một buổi thuộc người dùng này; rỗng nếu không có.</summary>
        Task<List<BankQuestion>> GetForSessionAsync(int sessionId, int userId, CancellationToken ct = default);

        /// <summary>
        /// Thay câu bộ đề cuối buổi (giữ câu 1 giới thiệu) bằng câu CV, gán mã cv-{sessionId}-{n} và lưu đáp án.
        /// Buổi phải đã được lưu (có Id). Lưu lỗi thì trả buổi về câu bộ đề ban đầu.
        /// </summary>
        /// <returns>Số câu CV đã gắn vào buổi (0 = vẫn 100% câu bộ đề).</returns>
        Task<int> AttachToSessionAsync(InterviewSession session, CvQuestionPipelineResult result, CancellationToken ct = default);
    }
}
