namespace MockviewAI.Models.ViewModels
{
    /// <summary>
    /// [PROFILE] 
    /// Dữ liệu trang Hồ sơ (Views/Profile/ViewProfile.cshtml).
    /// </summary>
    public class ProfileViewModel
    {
        public string FullName { get; set; } = string.Empty;

        /// <summary>Chữ cái đầu, hiện thay ảnh khi chưa có ảnh đại diện.</summary>
        public string AvatarInitial { get; set; } = "?";

        /// <summary>Dòng trạng thái dưới tên.</summary>
        public string StatusText { get; set; } = string.Empty;

        /// <summary>Ngành học.</summary>
        public string Major { get; set; } = string.Empty;

        /// <summary>Vị trí muốn ứng tuyển.</summary>
        public string TargetPosition { get; set; } = string.Empty;

        /// <summary>Mức kinh nghiệm.</summary>
        public string ExperienceLevel { get; set; } = string.Empty;

        /// <summary>Các buổi luyện gần nhất.</summary>
        public List<RecentSessionViewModel> RecentSessions { get; set; } = new();
    }
}
