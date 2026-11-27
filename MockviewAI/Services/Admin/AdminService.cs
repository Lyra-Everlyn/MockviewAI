using Microsoft.EntityFrameworkCore;
using MockviewAI.Data;
using MockviewAI.Models.Entities;
using MockviewAI.Models.ViewModels.Admin;

namespace MockviewAI.Services.Admin
{
    /// <summary>
    /// [ADMIN] 
    /// Đếm, gom nhóm, liệt kê dữ liệu cho trang /Admin.
    /// Dùng thẳng ApplicationDbContext vì cần Count / GroupBy / chọn cột (repository chung không hỗ trợ);
    /// mọi truy vấn đều AsNoTracking và không gọi SaveChanges.
    /// </summary>
    public class AdminService : IAdminService
    {
        #region Fields
        public const int PageSize = 20;
        private const int MaxSearchLength = 100;
        private const int ChartDays = 14;

        /// <summary>Các trạng thái tài khoản đang có trong bảng Users.</summary>
        public static readonly string[] Statuses = { "Active", "Locked", "Inactive" };

        private readonly ApplicationDbContext _db;
        #endregion

        #region Constructor
        public AdminService(ApplicationDbContext db)
        {
            _db = db;
        }
        #endregion

        #region Tổng quan
        public async Task<AdminDashboardViewModel> GetDashboardAsync()
        {
            var weekAgo = DateTime.UtcNow.AddDays(-7);
            var users = _db.Users.AsNoTracking();
            var sessions = _db.InterviewSessions.AsNoTracking();
            var feedbacks = _db.Feedbacks.AsNoTracking();

            // DbContext không chạy song song được: các truy vấn await lần lượt
            return new AdminDashboardViewModel
            {
                TotalUsers = await users.CountAsync(),
                NewUsers7Days = await users.CountAsync(u => u.CreateAt >= weekAgo),
                ActiveUsers7Days = await users.CountAsync(u => u.LastLoginAt != null && u.LastLoginAt >= weekAgo),

                TotalSessions = await sessions.CountAsync(),
                CompletedSessions = await sessions.CountAsync(s => s.Status == SessionStatus.Completed),
                Sessions7Days = await sessions.CountAsync(s => s.StartedAt >= weekAgo),
                AverageScore = await sessions
                    .Where(s => s.Status == SessionStatus.Completed && s.TotalScore != null)
                    .AverageAsync(s => (double?)s.TotalScore),

                TotalFeedbacks = await feedbacks.CountAsync(),
                AverageEaseRating = await feedbacks.AverageAsync(f => (double?)f.EaseRating),

                SessionsPerDay = await GetSessionsPerDayAsync(),
                TopPositions = await sessions
                    .GroupBy(s => s.Position)
                    .Select(g => new AdminPositionCount { Position = g.Key, Count = g.Count() })
                    .OrderByDescending(p => p.Count)
                    .Take(5)
                    .ToListAsync(),
                RecentUsers = await ProjectUsers(users.OrderByDescending(u => u.CreateAt).Take(5)),
                RecentFeedbacks = await ProjectFeedbacks(feedbacks.OrderByDescending(f => f.CreatedAt).Take(5))
            };
        }

        public async Task<AdminTabCounts> GetTabCountsAsync()
        {
            return new AdminTabCounts(
                await _db.Users.AsNoTracking().CountAsync(),
                await _db.Feedbacks.AsNoTracking().CountAsync());
        }

        /// <summary>
        /// Số buổi luyện mỗi ngày (giờ Việt Nam) trong 14 ngày gần nhất, kể cả ngày không có buổi nào.
        /// Gom nhóm trong bộ nhớ (chỉ lấy một cột) để không lệch ngày giữa UTC và giờ Việt Nam.
        /// </summary>
        private async Task<List<AdminDailyCount>> GetSessionsPerDayAsync()
        {
            var since = DateTime.UtcNow.AddDays(-(ChartDays + 1));
            var startedTimes = await _db.InterviewSessions.AsNoTracking()
                .Where(s => s.StartedAt >= since)
                .Select(s => s.StartedAt)
                .ToListAsync();

            var counts = startedTimes
                .GroupBy(t => VietnamTime.FromUtc(t).Date)
                .ToDictionary(g => g.Key, g => g.Count());

            var today = VietnamTime.Today;
            return Enumerable.Range(0, ChartDays)
                .Select(i => today.AddDays(i - ChartDays + 1))
                .Select(day => new AdminDailyCount { Day = day, Count = counts.GetValueOrDefault(day) })
                .ToList();
        }
        #endregion

        #region Người dùng
        public async Task<AdminUserListViewModel> GetUsersAsync(string? search, string? status, int page)
        {
            search = search?.Trim();
            if (search?.Length > MaxSearchLength) search = search[..MaxSearchLength];
            if (!Statuses.Contains(status)) status = null;

            var query = _db.Users.AsNoTracking();
            if (!string.IsNullOrEmpty(search))
            {
                // Email luôn lưu chữ thường (AuthService), tên giữ nguyên
                string lower = search.ToLowerInvariant();
                query = query.Where(u => u.Email.Contains(lower)
                                      || u.FirstName.Contains(search)
                                      || (u.LastName != null && u.LastName.Contains(search)));
            }
            if (status != null) query = query.Where(u => u.Status == status);

            int total = await query.CountAsync();
            page = ClampPage(page, total);

            return new AdminUserListViewModel
            {
                Users = await ProjectUsers(query.OrderByDescending(u => u.CreateAt).Skip((page - 1) * PageSize).Take(PageSize)),
                Search = search,
                Status = status,
                Page = page,
                PageSize = PageSize,
                TotalCount = total
            };
        }
        #endregion

        #region Góp ý
        public async Task<AdminFeedbackListViewModel> GetFeedbacksAsync(int? rating, bool onlyWithComment, int page)
        {
            if (rating is < 1 or > 5) rating = null;

            var query = _db.Feedbacks.AsNoTracking();
            if (rating != null) query = query.Where(f => f.EaseRating == rating);
            if (onlyWithComment) query = query.Where(f => f.Comment != null && f.Comment != "");

            int total = await query.CountAsync();
            page = ClampPage(page, total);

            return new AdminFeedbackListViewModel
            {
                Feedbacks = await ProjectFeedbacks(query.OrderByDescending(f => f.CreatedAt).Skip((page - 1) * PageSize).Take(PageSize)),
                Rating = rating,
                OnlyWithComment = onlyWithComment,
                Page = page,
                PageSize = PageSize,
                TotalCount = total
            };
        }
        #endregion

        #region Helpers
        public static string StatusLabel(string? status) => status switch
        {
            "Active" => "Hoạt động",
            "Inactive" => "Đình chỉ",
            "Locked" => "Đã khoá",
            _ => status ?? string.Empty
        };

        private static int ClampPage(int page, int total) =>
            Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(total / (double)PageSize)));

        private async Task<List<AdminUserRowViewModel>> ProjectUsers(IQueryable<User> query)
        {
            var rows = await query
                .Select(u => new AdminUserRowViewModel
                {
                    Email = u.Email,
                    FullName = ((u.LastName ?? "") + " " + u.FirstName).Trim(),
                    AvatarUrl = u.AvatarUrl,
                    Status = u.Status,
                    CreateAt = u.CreateAt,
                    LastLoginAt = u.LastLoginAt,
                    SessionCount = _db.InterviewSessions.Count(s => s.UserId == u.Id)
                })
                .ToListAsync();

            foreach (var r in rows)
            {
                r.CreateAt = VietnamTime.FromUtc(r.CreateAt);
                r.LastLoginAt = VietnamTime.FromUtc(r.LastLoginAt);
            }
            return rows;
        }

        private static async Task<List<AdminFeedbackRowViewModel>> ProjectFeedbacks(IQueryable<Feedback> query)
        {
            var rows = await query
                .Select(f => new AdminFeedbackRowViewModel
                {
                    UserEmail = f.User != null ? f.User.Email : "",
                    UserName = f.User != null ? ((f.User.LastName ?? "") + " " + f.User.FirstName).Trim() : "",
                    EaseRating = f.EaseRating,
                    AnxietyHelp = f.AnxietyHelp,
                    Comment = f.Comment,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();

            foreach (var r in rows) r.CreatedAt = VietnamTime.FromUtc(r.CreatedAt);
            return rows;
        }
        #endregion
    }
}
