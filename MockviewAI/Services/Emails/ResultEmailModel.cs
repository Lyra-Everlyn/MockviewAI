namespace MockviewAI.Services.Emails
{
    /// <summary>
    /// [EMAIL] 
    /// Dữ liệu email kết quả buổi luyện; InterviewService dựng, ResultEmailBuilder soạn HTML.
    /// </summary>
    public class ResultEmailModel
    {
        #region Thông tin buổi
        /// <summary>Tên người nhận, dùng trong lời chào.</summary>
        public string FirstName { get; set; } = string.Empty;

        public string Position { get; set; } = string.Empty;
        public string InterviewerName { get; set; } = string.Empty;

        /// <summary>Giờ bắt đầu (giờ địa phương).</summary>
        public DateTime StartedAt { get; set; }

        public int QuestionCount { get; set; }
        #endregion

        #region Điểm
        public int TotalScore { get; set; }

        /// <summary>null = AI lỗi, chấm dự phòng chỉ tính cách nói.</summary>
        public int? ContentScore { get; set; }

        public int DeliveryScore { get; set; }

        /// <summary>Dấu hiệu lo lắng 1–10; 0 = chưa đủ dữ liệu.</summary>
        public int AnxietyIndicator { get; set; }

        public List<string> AnxietyReasons { get; set; } = new();
        #endregion

        #region Nhận xét
        public string Summary { get; set; } = string.Empty;
        public List<string> Strengths { get; set; } = new();
        public List<string> Improvements { get; set; } = new();
        public List<string> DeliveryTips { get; set; } = new();
        #endregion
    }
}
