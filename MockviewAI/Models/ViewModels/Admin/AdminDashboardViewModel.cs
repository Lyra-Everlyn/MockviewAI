namespace MockviewAI.Models.ViewModels.Admin
{
    /// <summary>
    /// [ADMIN] 
    /// Trang Tổng quan: số liệu chính, biểu đồ 14 ngày, top vị trí, người dùng mới, góp ý gần đây.
    /// </summary>
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int NewUsers7Days { get; set; }

        /// <summary>Số người đăng nhập trong 7 ngày qua.</summary>
        public int ActiveUsers7Days { get; set; }

        public int TotalSessions { get; set; }
        public int CompletedSessions { get; set; }
        public int Sessions7Days { get; set; }

        /// <summary>Điểm trung bình các buổi đã chấm (thang 100); null khi chưa có buổi nào.</summary>
        public double? AverageScore { get; set; }

        public int TotalFeedbacks { get; set; }

        /// <summary>Số sao "dễ dùng" trung bình (1–5); null khi chưa có góp ý.</summary>
        public double? AverageEaseRating { get; set; }

        public List<AdminDailyCount> SessionsPerDay { get; set; } = new();
        public List<AdminPositionCount> TopPositions { get; set; } = new();
        public List<AdminUserRowViewModel> RecentUsers { get; set; } = new();
        public List<AdminFeedbackRowViewModel> RecentFeedbacks { get; set; } = new();
    }

    /// <summary>[ADMIN] Số buổi luyện của một ngày (theo giờ Việt Nam).</summary>
    public class AdminDailyCount
    {
        public DateTime Day { get; set; }
        public int Count { get; set; }
    }

    /// <summary>[ADMIN] Số buổi luyện của một vị trí ứng tuyển.</summary>
    public class AdminPositionCount
    {
        public string Position { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    /// <summary>[ADMIN] Một dòng trong bảng người dùng (giờ đã đổi sang giờ Việt Nam).</summary>
    public class AdminUserRowViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreateAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int SessionCount { get; set; }

        public string DisplayName => string.IsNullOrWhiteSpace(FullName) ? Email : FullName;
    }

    /// <summary>[ADMIN] Trang Người dùng: tìm kiếm, lọc trạng thái, phân trang.</summary>
    public class AdminUserListViewModel
    {
        public List<AdminUserRowViewModel> Users { get; set; } = new();
        public string? Search { get; set; }
        public string? Status { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    }

    /// <summary>[ADMIN] Một góp ý (giờ đã đổi sang giờ Việt Nam).</summary>
    public class AdminFeedbackRowViewModel
    {
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int EaseRating { get; set; }
        public string AnxietyHelp { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>[ADMIN] Trang Góp ý: lọc số sao / chỉ góp ý có nội dung, phân trang.</summary>
    public class AdminFeedbackListViewModel
    {
        public List<AdminFeedbackRowViewModel> Feedbacks { get; set; } = new();
        public int? Rating { get; set; }
        public bool OnlyWithComment { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    }

    /// <summary>[ADMIN] Tổng số người dùng và góp ý, hiện cạnh tên tab ở mọi trang quản trị.</summary>
    public record AdminTabCounts(int Users, int Feedbacks);
}
