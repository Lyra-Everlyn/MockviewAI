using MockviewAI.Models.ViewModels;

namespace MockviewAI.Services.SessionHistory
{
    public interface ISessionHistoryService
    {
        Task<List<RecentSessionViewModel>> GetHistoryAsync(string email, int? take = null);
        Task<InterviewDetailViewModel?> GetDetailAsync(string email, int sessionId);
        Task<bool> DeleteAsync(string email, int sessionId);
    }
}
