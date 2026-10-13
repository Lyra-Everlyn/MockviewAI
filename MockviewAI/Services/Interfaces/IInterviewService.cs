// [AI-INTERVIEW] Các việc của một buổi luyện phỏng vấn: bắt đầu, trả lời, kết thúc, xem kết quả.
// Mọi hàm đều nhận email người đăng nhập để chắc chắn người dùng chỉ thấy buổi luyện của mình.

using MockviewAI.Models.DTOs;
using MockviewAI.Models.ViewModels;

namespace MockviewAI.Services.Interfaces
{
    public interface IInterviewService
    {
        // Tạo buổi luyện mới, trả về Id của buổi
        Task<int> StartAsync(string email, string positionKey, string interviewerKey, int questionCount, int? anxietyBefore);

        // Lấy câu hỏi hiện tại để vẽ phòng phỏng vấn. Trả về null nếu không tìm thấy buổi, hoặc buổi đã kết thúc.
        Task<InterviewRoomViewModel?> GetRoomAsync(string email, int sessionId);

        // Lưu câu trả lời (hoặc bỏ qua) của câu hiện tại
        Task<SubmitAnswerResultDto> SubmitAnswerAsync(string email, SubmitAnswerDto dto);

        // Kết thúc buổi và chấm điểm
        Task FinishAsync(string email, FinishSessionDto dto);

        // Lấy dữ liệu trang kết quả. Trả về null nếu không tìm thấy buổi.
        Task<SessionResultViewModel?> GetResultAsync(string email, int sessionId);
    }
}
