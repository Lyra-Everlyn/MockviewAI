using MockviewAI.Models.DTOs;

namespace MockviewAI.Models.ViewModels
{
    /// <summary>
    /// [USER CV] 
    /// Trang tải CV và kiểm tra kết quả phân tích (Views/CV/UploadCV.cshtml).
    /// </summary>
    public class CvUploadViewModel : CvPageViewModel
    {
        /// <summary>File CV tải lên.</summary>
        public IFormFile? CvFile { get; set; }

        /// <summary>Dữ liệu AI trích xuất, để người dùng sửa trước khi chốt.</summary>
        public CvParsedDataDto? ParsedData { get; set; }

        /// <summary>true = đã phân tích xong, hiện form kiểm tra.</summary>
        public bool IsParsed { get; set; }

        /// <summary>Lỗi kiểm tra file hoặc phân tích; rỗng = không lỗi.</summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
