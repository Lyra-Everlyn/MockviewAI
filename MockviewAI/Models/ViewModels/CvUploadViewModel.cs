using MockviewAI.Models.DTOs;

namespace MockviewAI.Models.ViewModels
{
    /// <summary>
    /// [USER CV] Trang tải CV và kiểm tra kết quả phân tích.
    /// </summary>
    public class CvUploadViewModel : CvPageViewModel
    {
        public IFormFile? CvFile { get; set; }
        public CvParsedDataDto? ParsedData { get; set; }
        public bool IsParsed { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
