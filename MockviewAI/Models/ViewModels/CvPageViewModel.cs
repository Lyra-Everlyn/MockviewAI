namespace MockviewAI.Models.ViewModels
{
    /// <summary>
    /// [USER CV] Thông tin chung để CvStorage.js lưu CV trên trình duyệt.
    /// </summary>
    public class CvPageViewModel
    {
        public string OwnerKey { get; set; } = string.Empty;
        public int MaxCvs { get; set; } = 3;
        public int KeepDays { get; set; } = 30;
    }
}
