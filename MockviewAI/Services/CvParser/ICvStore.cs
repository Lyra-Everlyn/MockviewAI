using MockviewAI.Models.DTOs;

namespace MockviewAI.Services.CvParser
{
    /// <summary>
    /// [USER CV] 
    /// Giữ tạm CV vừa phân tích, chờ người dùng xác nhận. CV đã lưu nằm trên trình duyệt.
    /// </summary>
    public interface ICvStore
    {
        Task SavePendingAsync(string email, CvParsedDataDto data, CancellationToken ct = default);
        Task<CvParsedDataDto?> GetPendingAsync(string email, CancellationToken ct = default);
        Task RemovePendingAsync(string email, CancellationToken ct = default);
    }
}
