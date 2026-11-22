using MockviewAI.Models.Entities;
using MockviewAI.Models.ViewModels;

namespace MockviewAI.Services.Interviews.Results
{
    /// <summary>
    /// [HISTORY] 
    /// Dựng dữ liệu trang Kết quả của một buổi luyện đã kết thúc.
    /// </summary>
    public interface ISessionResultBuilder
    {
        /// <param name="session">Buổi đã kết thúc, đã nạp kèm danh sách câu trả lời.</param>
        Task<SessionResultViewModel> BuildAsync(InterviewSession session);
    }
}
