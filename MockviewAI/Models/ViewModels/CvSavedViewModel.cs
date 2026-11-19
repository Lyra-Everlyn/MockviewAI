namespace MockviewAI.Models.ViewModels
{
    /// <summary>
    /// [USER CV] Trang trung gian mang CV đã chốt (JSON) để trình duyệt lưu lại.
    /// </summary>
    public class CvSavedViewModel : CvPageViewModel
    {
        public string CvJson { get; set; } = "{}";
    }
}
