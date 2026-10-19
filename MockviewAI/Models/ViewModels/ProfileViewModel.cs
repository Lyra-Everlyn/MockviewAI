namespace MockviewAI.Models.ViewModels
{
    // Dữ liệu trang Hồ sơ (Profile.cshtml).
    public class ProfileViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string AvatarInitial { get; set; } = "?";
        public string StatusText { get; set; } = string.Empty;      // vd: "Sắp ra trường"
        public string Major { get; set; } = string.Empty;
        public string TargetPosition { get; set; } = string.Empty;
        public List<RecentSessionViewModel> RecentSessions { get; set; } = new();
    }
}
