using MockviewAI.Models.DTOs;

namespace MockviewAI.Services.Feedbacks
{
    /// <summary>
    /// [FEEDBACK] 
    /// Lưu góp ý của người dùng.
    /// </summary>
    public interface IFeedbackService
    {
        /// <returns>false = không tìm thấy tài khoản (controller chuyển về trang đăng nhập).</returns>
        Task<bool> SubmitAsync(string email, FeedbackDto dto);
    }
}
