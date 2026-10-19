using MockviewAI.Models.DTOs;

namespace MockviewAI.Services.Interfaces
{
    // Lưu góp ý cho bản thử V1.
    public interface IFeedbackService
    {
        Task<bool> SubmitAsync(string email, FeedbackDto dto);
    }
}
