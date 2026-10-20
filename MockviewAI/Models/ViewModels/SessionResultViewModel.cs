namespace MockviewAI.Models.ViewModels
{
    /// [AI-INTERVIEW]
    // -- Dữ liệu trang kết quả. Tên thuộc tính khớp với Result.cshtml của giao diện:
    // -- TotalScore, ContentScore, DeliveryScore, AnxietyBefore, AnxietyAfter, SessionId.
    public class SessionResultViewModel
    {
        public int SessionId { get; set; }
        public string Position { get; set; } = string.Empty;
        public string InterviewerName { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public int QuestionCount { get; set; }

        public bool HasScore { get; set; }
        public int TotalScore { get; set; }
        public int ContentScore { get; set; }
        public int DeliveryScore { get; set; }
        public int AnxietyBefore { get; set; }
        public int AnxietyAfter { get; set; }

        public string AiSummary { get; set; } = string.Empty;
        public List<string> Strengths { get; set; } = new();
        public List<string> Improvements { get; set; } = new();
        public List<string> DeliveryTips { get; set; } = new();
    }
}
