namespace MockviewAI.Models.ViewModels
{
    /// [AI-INTERVIEW]
    // -- Dữ liệu trang kết quả (Result.cshtml).
    public class SessionResultViewModel
    {
        public int SessionId { get; set; }
        public string Position { get; set; } = string.Empty;
        public string InterviewerName { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public int QuestionCount { get; set; }

        public bool HasScore { get; set; }
        public bool HasContentScore { get; set; }    // false: AI lỗi, đây là kết quả dự phòng chỉ tính cách nói
        public int TotalScore { get; set; }
        public int ContentScore { get; set; }
        public int DeliveryScore { get; set; }

        // Dấu hiệu lo lắng 1-10 do hệ thống ước tính từ cách nói. 0 = chưa đủ dữ liệu.
        public int AnxietyIndicator { get; set; }
        public int PreviousAnxietyIndicator { get; set; }   // buổi trước (0 = chưa có)
        public string AnxietyLevelText => AnxietyIndicator switch
        {
            <= 0 => string.Empty,
            <= 3 => "Thấp",
            <= 6 => "Trung bình",
            _ => "Cao"
        };

        public bool EmailRequested { get; set; }

        public string AiSummary { get; set; } = string.Empty;
        public List<string> Strengths { get; set; } = new();
        public List<string> Improvements { get; set; } = new();
        public List<string> DeliveryTips { get; set; } = new();
    }
}
