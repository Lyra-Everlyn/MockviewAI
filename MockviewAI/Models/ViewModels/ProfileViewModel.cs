using System.ComponentModel;

namespace MockviewAI.Models.ViewModels
{
    /// [PROFILE]
    public class ProfileViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string AvatarInitial { get; set; } = "?";
        public string StatusText { get; set; } = string.Empty;
        public string Major { get; set; } = string.Empty;
        public string TargetPosition { get; set; } = string.Empty;
        public string ExperienceLevel { get; set; } = string.Empty;
        public List<RecentSessionViewModel> RecentSessions { get; set; } = new();
    }
}