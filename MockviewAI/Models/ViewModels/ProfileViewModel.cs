namespace MockviewAI.Models.ViewModels
{
    /// <summary>
    /// [PROFILE] 
    /// Dữ liệu trang Hồ sơ (Views/Profile/ViewProfile.cshtml).
    /// </summary>
    public class ProfileViewModel
    {
        #region Thông tin cá nhân
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
        #endregion

        #region Hành trình
        /// <summary>Ngày tạo tài khoản (giờ Việt Nam).</summary>
        public DateTime MemberSince { get; set; }

        /// <summary>Số ngày kể từ khi tạo tài khoản, tính cả ngày đầu.</summary>
        public int DaysWithUs { get; set; }

        /// <summary>Giờ đăng nhập của phiên hiện tại (AuthService ghi LastLoginAt khi đăng nhập), giờ Việt Nam.</summary>
        public DateTime? CurrentLoginAt { get; set; }

        public int TotalSessions { get; set; }

        public int AnsweredQuestions { get; set; }

        public int StreakDays { get; set; }
        #endregion
    }
}
