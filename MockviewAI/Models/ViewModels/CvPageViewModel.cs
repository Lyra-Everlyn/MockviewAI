namespace MockviewAI.Models.ViewModels
{
    /// <summary>
    /// [USER CV] 
    /// Thông tin chung mọi trang CV cần để CvStorage.js lưu CV trên trình duyệt.
    /// </summary>
    public class CvPageViewModel
    {
        /// <summary>Khoá chủ sở hữu (băm từ email), để CV của tài khoản này không lẫn với tài khoản khác trên cùng trình duyệt.</summary>
        public string OwnerKey { get; set; } = string.Empty;

        /// <summary>Số CV tối đa mỗi người dùng lưu được.</summary>
        public int MaxCvs { get; set; } = 3;

        /// <summary>Số ngày trình duyệt giữ CV.</summary>
        public int KeepDays { get; set; } = 30;
    }
}
