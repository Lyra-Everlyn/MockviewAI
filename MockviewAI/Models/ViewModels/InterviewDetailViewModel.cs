namespace MockviewAI.Models.ViewModels
{
    // Dữ liệu trang Xem lại hội thoại (InterviewDetail.cshtml).
    public class InterviewDetailViewModel
    {
        public int SessionId { get; set; }
        public string Position { get; set; } = string.Empty;
        public string InterviewerName { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public int QuestionCount { get; set; }

        public bool HasScore { get; set; }          // false: buổi dừng trước khi có điểm
        public bool HasContentScore { get; set; }   // false: kết quả dự phòng (AI lỗi)
        public int TotalScore { get; set; }
        public int ContentScore { get; set; }
        public int DeliveryScore { get; set; }
        public int AnxietyIndicator { get; set; }   // 0 = chưa có

        public string AiSummary { get; set; } = string.Empty;
        public List<string> Improvements { get; set; } = new();
        public List<ConversationTurnViewModel> Turns { get; set; } = new();
    }
}
