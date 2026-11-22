using MockviewAI.Models.DTOs;

namespace MockviewAI.Services.CvParser
{
    /// <summary>
    /// [USER CV] 
    /// Điều phối luồng CV: đọc PDF -> AI phân tích -> người dùng kiểm tra -> chốt.
    /// </summary>
    public interface ICvService
    {
        /// <summary>Tách chữ, gửi AI, làm sạch rồi giữ bản chờ trong Redis. Lỗi nghiệp vụ ném CvException.</summary>
        /// <param name="file">File đã qua ICvFileValidator.</param>
        Task<CvParsedDataDto> AnalyzeAsync(string email, IFormFile file, CancellationToken ct = default);

        /// <summary>
        /// Chốt CV: chỉ nhận họ tên, chức danh, mục tiêu và kỹ năng từ form; phần còn lại lấy từ bản chờ phía server.
        /// </summary>
        /// <param name="hardSkillsCsv">Kỹ năng ngăn bằng dấu phẩy, chấm phẩy hoặc xuống dòng.</param>
        /// <returns>CV đã chốt để trình duyệt lưu; null nếu bản chờ đã hết hạn.</returns>
        Task<CvParsedDataDto?> ConfirmAsync(string email, CvParsedDataDto edited, string? hardSkillsCsv, CancellationToken ct = default);
    }
}
