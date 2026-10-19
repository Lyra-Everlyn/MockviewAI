namespace MockviewAI.Models.ViewModels
{
    // Một lượt hỏi - đáp trong trang Xem lại hội thoại.
    public class ConversationTurnViewModel
    {
        public int Number { get; set; }
        public string Question { get; set; } = string.Empty;
        public string? Answer { get; set; }
        public bool Skipped { get; set; }
    }
}
