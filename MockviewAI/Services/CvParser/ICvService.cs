using MockviewAI.Models.DTOs;

namespace MockviewAI.Services.CvParser
{
    /// <summary>
    /// [USER CV] Điều phối luồng CV: đọc PDF -> AI phân tích -> người dùng xác nhận.
    /// </summary>
    public interface ICvService
    {
        Task<CvParsedDataDto> AnalyzeAsync(string email, IFormFile file, CancellationToken ct = default);

        /// <summary>Trả về CV đã chốt để trình duyệt lưu; null nếu bản chờ đã hết hạn.</summary>
        Task<CvParsedDataDto?> ConfirmAsync(string email, CvParsedDataDto edited, string? hardSkillsCsv, CancellationToken ct = default);
    }
}
