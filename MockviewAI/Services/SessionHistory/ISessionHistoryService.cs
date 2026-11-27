using MockviewAI.Models.ViewModels;

namespace MockviewAI.Services.SessionHistory
{
    /// <summary>
    /// [HISTORY] 
    /// Lịch sử luyện tập, xem lại hội thoại và xoá buổi. Buổi của người khác coi như không tồn tại.
    /// </summary>
    public interface ISessionHistoryService
    {
        /// <summary>Các buổi mới nhất trước.</summary>
        /// <param name="take">Số buổi lấy; null = tất cả.</param>
        Task<List<RecentSessionViewModel>> GetHistoryAsync(string email, int? take = null);

        /// <summary>Từng lượt hỏi – đáp kèm xếp loại; null = không tìm thấy.</summary>
        Task<InterviewDetailViewModel?> GetDetailAsync(string email, int sessionId);

        /// <returns>false = không tìm thấy buổi.</returns>
        Task<bool> DeleteAsync(string email, int sessionId);

        /// <summary>Số liệu tổng hợp cho thẻ Hành trình (trang Hồ sơ).</summary>
        Task<PracticeJourneyStats> GetJourneyStatsAsync(string email);
    }
}
