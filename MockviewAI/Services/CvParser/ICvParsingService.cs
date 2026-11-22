using MockviewAI.Models.DTOs;

namespace MockviewAI.Services.CvParser
{
    /// <summary>
    /// [USER CV] 
    /// Dùng AI chuyển nội dung CV thành dữ liệu có cấu trúc.
    /// </summary>
    public interface ICvParsingService
    {
        /// <summary>Đủ chữ thì gửi văn bản, không đủ (PDF scan) thì gửi nguyên file PDF.</summary>
        Task<CvParsedDataDto> ParseAsync(byte[] pdf, string extractedText, CancellationToken ct = default);
    }
}
