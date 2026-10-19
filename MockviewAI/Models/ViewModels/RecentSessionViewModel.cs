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
    }
}
