using MockviewAI.Models.Ai;
using MockviewAI.Models.Entities;

namespace MockviewAI.Services.Interviews.Results
{
    /// <summary>
    /// [EMAIL] 
    /// Gửi email kết quả buổi luyện (khi người dùng chọn nhận).
    /// </summary>
    public interface IResultEmailSender
    {
        /// <summary>Gửi ở nền, không chờ: lỗi SMTP chỉ ghi log, không làm chậm hay hỏng việc xem kết quả.</summary>
        void SendInBackground(User user, InterviewSession session, InterviewScoreResult result);
    }
}
