namespace MockviewAI.Models.ViewModels
{
    public class HomeViewModel
    {
        public bool IsAuthenticated { get; set; }

        public string FullName { get; set; } = "Khách";
        public string AvatarInitial { get; set; } = "G";
        public string? AvatarUrl { get; set; }
        public string GreetingSub { get; set; } = "Chào buổi sáng";

        public List<RecentSessionViewModel> RecentSessions { get; set; } = new List<RecentSessionViewModel>();
    }
}
