using MockviewAI.Models.DTOs;

namespace MockviewAI.Services.Feedbacks
{
    public interface IFeedbackService
    {
        Task<bool> SubmitAsync(string email, FeedbackDto dto);
    }
}
