using MockviewAI.Models.DTOs;
using MockviewAI.Models.ViewModels;

namespace MockviewAI.Services.Interviews
{
    /// [AI-INTERVIEW]
    public interface IInterviewService
    {
        // Tạo buổi luyện mới, trả về Id của buổi. cvJson: CV mặc định trên trình duyệt (null = không dùng CV)
        Task<int> StartAsync(string email, string positionKey, string interviewerKey, int questionCount, bool emailResult, string? mode = null, string? cvJson = null);

        // Lấy câu hỏi hiện tại để vẽ phòng phỏng vấn. Trả về null nếu không tìm thấy buổi, hoặc buổi đã kết thúc.
        Task<InterviewRoomViewModel?> GetRoomAsync(string email, int sessionId);

        // Lưu câu trả lời (hoặc bỏ qua) của câu hiện tại, kèm phản hồi của nhà tuyển dụng
        Task<SubmitAnswerResultDto> SubmitAnswerAsync(string email, SubmitAnswerDto dto);

        // Kết thúc buổi và chấm điểm
        Task FinishAsync(string email, FinishSessionDto dto);

        // Lấy dữ liệu trang kết quả. Trả về null nếu không tìm thấy buổi.
        Task<SessionResultViewModel?> GetResultAsync(string email, int sessionId);

        /// <summary>
        /// [SPEECH] Lấy nội dung câu hỏi hiện tại (câu chính hoặc câu hỏi thêm) của buổi đang diễn ra,
        /// dùng làm ngữ cảnh cho chuyển giọng nói thành chữ. Đồng thời kiểm tra buổi thuộc người dùng và còn đang làm.
        /// </summary>
        /// <exception cref="Exceptions.InterviewException">Không tìm thấy buổi, buổi đã kết thúc hoặc đã hết câu hỏi.</exception>
        Task<string> GetCurrentQuestionAsync(string email, int sessionId);
    }
}
