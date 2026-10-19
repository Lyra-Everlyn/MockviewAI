namespace MockviewAI.Models.ViewModels
{
    public class RecentSessionViewModel
    {
        public int SessionId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string DateFormatted { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
        public int Score { get; set; }
        public string AnxietyText { get; set; } = string.Empty;
        public bool AnxietyImproved { get; set; }   // true: badge xanh, false: badge cam
        public bool HasScore { get; set; }          // false: buổi dừng sớm, chưa có điểm
        public bool IsInProgress { get; set; }      // true: buổi chưa xong, bấm vào thì luyện tiếp
        public string StatusText { get; set; } = string.Empty;   // "Đã dừng" / "Chưa xong"
    }
}
