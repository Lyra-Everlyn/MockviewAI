namespace MockviewAI.Models.ViewModels
{
    /// <summary>
    /// [HISTORY] 
    /// Dữ liệu trang Xem lại hội thoại (Views/Home/InterviewDetail.cshtml).
    /// </summary>
    public class InterviewDetailViewModel
    {
        #region Thông tin buổi
        public int SessionId { get; set; }
        public string Position { get; set; } = string.Empty;
        public string InterviewerName { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public int QuestionCount { get; set; }

        /// <summary>[LIVE-MODE] Buổi Thực chiến.</summary>
        public bool IsLive { get; set; }
        #endregion

        #region Điểm
        /// <summary>false = buổi dừng trước khi có điểm.</summary>
        public bool HasScore { get; set; }

        /// <summary>false = kết quả dự phòng (AI lỗi).</summary>
        public bool HasContentScore { get; set; }

        public int TotalScore { get; set; }
        public int ContentScore { get; set; }
        public int DeliveryScore { get; set; }

        /// <summary>Dấu hiệu lo lắng 1–10; 0 = chưa có.</summary>
        public int AnxietyIndicator { get; set; }

        public string AiSummary { get; set; } = string.Empty;
        public List<string> Improvements { get; set; } = new();
        #endregion

        /// <summary>Các lượt hỏi – đáp theo thứ tự.</summary>
        public List<ConversationTurnViewModel> Turns { get; set; } = new();
    }
}
