using MockviewAI.Models.DTOs;

namespace MockviewAI.Models.ViewModels
{
    /// <summary>
    /// [USER CV]
    /// -- Hiện thị dữ liệu parse được từ CV
    /// </summary>
    public class CvUploadViewModel
    {
        public IFormFile CvFile { get; set; }
        public CvParsedDataDto ParsedData { get; set; }
        public bool IsParsed { get; set; } = false;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
