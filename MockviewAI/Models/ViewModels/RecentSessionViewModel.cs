namespace MockviewAI.Models.ViewModels
{
    /// <summary>
    /// [HISTORY] 
    /// Một thẻ buổi luyện trong Trang chủ, Lịch sử và Hồ sơ (Partials/_SessionCard.cshtml).
    /// </summary>
    public class RecentSessionViewModel
    {
        public int SessionId { get; set; }

        /// <summary>Tên vị trí.</summary>
        public string RoleName { get; set; } = string.Empty;

        /// <summary>Ngày giờ đã định dạng sẵn.</summary>
        public string DateFormatted { get; set; } = string.Empty;

        public int QuestionCount { get; set; }
        public int Score { get; set; }

        /// <summary>Nhãn dấu hiệu lo lắng.</summary>
        public string AnxietyText { get; set; } = string.Empty;

        /// <summary>true: badge xanh, false: badge cam.</summary>
        public bool AnxietyImproved { get; set; }

        /// <summary>false = buổi dừng sớm, chưa có điểm.</summary>
        public bool HasScore { get; set; }

        /// <summary>true = buổi chưa xong, bấm vào để luyện tiếp.</summary>
        public bool IsInProgress { get; set; }

        /// <summary>"Đã dừng" / "Chưa xong".</summary>
        public string StatusText { get; set; } = string.Empty;

        /// <summary>[LIVE-MODE] Buổi Thực chiến, hiện nhãn trong Lịch sử.</summary>
        public bool IsLive { get; set; }
    }
}
