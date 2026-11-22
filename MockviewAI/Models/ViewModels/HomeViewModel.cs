namespace MockviewAI.Models.ViewModels
{
    /// <summary>
    /// [KHÔNG DÙNG] [HOME] 
    /// ViewModel trang chủ đời đầu. Trang chủ hiện nhận List&lt;RecentSessionViewModel&gt;
    /// và tự lấy tên / ảnh từ Claims, không còn chỗ nào tham chiếu lớp này.
    /// </summary>
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
