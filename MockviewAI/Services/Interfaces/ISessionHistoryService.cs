using MockviewAI.Models.ViewModels;

namespace MockviewAI.Services.Interfaces
{
    // Lịch sử buổi luyện: danh sách, xem lại hội thoại, xoá buổi.
    public interface ISessionHistoryService
    {
        Task<List<RecentSessionViewModel>> GetHistoryAsync(string email, int? take = null);
        Task<InterviewDetailViewModel?> GetDetailAsync(string email, int sessionId);
        Task<bool> DeleteAsync(string email, int sessionId);
    }
}
