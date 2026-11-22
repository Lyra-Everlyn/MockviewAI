using MockviewAI.Models.DTOs;
using MockviewAI.Models.ViewModels;

namespace MockviewAI.Services.Interviews
{
    /// <summary>
    /// [AI-INTERVIEW] 
    /// Vòng đời một buổi luyện. Mọi hàm chỉ thao tác trên buổi của chính người dùng (theo email);
    /// lỗi nghiệp vụ ném InterviewException (message hiện được cho người dùng).
    /// </summary>
    public interface IInterviewService
    {
        /// <summary>Tạo buổi luyện mới (tối đa 10 buổi / giờ / tài khoản).</summary>
        /// <param name="cvJson">CV mặc định trên trình duyệt; null = không hỏi theo CV.</param>
        /// <returns>Id của buổi.</returns>
        Task<int> StartAsync(string email, string positionKey, string interviewerKey, int questionCount, bool emailResult, string? mode = null, string? cvJson = null);

        /// <summary>Câu hỏi hiện tại để vẽ phòng phỏng vấn; null = không tìm thấy buổi, buổi đã kết thúc hoặc đã hết câu.</summary>
        Task<InterviewRoomViewModel?> GetRoomAsync(string email, int sessionId);

        /// <summary>
        /// [SPEECH] 
        /// Nội dung câu hỏi hiện tại (câu chính hoặc câu hỏi thêm), làm ngữ cảnh cho chuyển giọng nói thành chữ.
        /// Đồng thời kiểm tra buổi thuộc người dùng và còn đang làm. Không ghi gì vào database.
        /// </summary>
        /// <exception cref="Exceptions.InterviewException">Không tìm thấy buổi, buổi đã kết thúc hoặc đã hết câu hỏi.</exception>
        Task<string> GetCurrentQuestionAsync(string email, int sessionId);

        /// <summary>Lưu câu trả lời (hoặc bỏ qua) của câu hiện tại, kèm phản hồi của nhà tuyển dụng và câu hỏi thêm nếu có.</summary>
        /// <exception cref="Exceptions.AnswerRejectedException">Câu trả lời vô nghĩa, được trả lời lại (chế độ Luyện tập).</exception>
        Task<SubmitAnswerResultDto> SubmitAnswerAsync(string email, SubmitAnswerDto dto);

        /// <summary>Kết thúc buổi và chấm điểm; buổi đã kết thúc thì bỏ qua. Chưa trả lời câu nào thì chuyển sang Stopped.</summary>
        /// <exception cref="Exceptions.AiScoringException">AI lỗi khi không dùng Fallback; buổi vẫn InProgress để chấm lại.</exception>
        Task FinishAsync(string email, FinishSessionDto dto);

        /// <summary>Dữ liệu trang Kết quả; null = không tìm thấy buổi hoặc buổi còn đang làm.</summary>
        Task<SessionResultViewModel?> GetResultAsync(string email, int sessionId);
    }
}
