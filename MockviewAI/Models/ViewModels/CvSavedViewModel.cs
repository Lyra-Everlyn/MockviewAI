namespace MockviewAI.Models.ViewModels
{
    /// <summary>
    /// [USER CV] 
    /// Trang trung gian mang CV đã chốt để trình duyệt lưu lại (Views/CV/Saved.cshtml).
    /// </summary>
    public class CvSavedViewModel : CvPageViewModel
    {
        /// <summary>CV đã chốt, JSON camelCase.</summary>
        public string CvJson { get; set; } = "{}";
    }
}
