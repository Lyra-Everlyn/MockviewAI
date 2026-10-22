namespace MockviewAI.Models.DTOs
{
    /// [AI-INTERVIEW] 
    // -- Người dùng gửi lên khi trả lời hoặc bỏ qua một câu
    public class SubmitAnswerDto
    {
        public int SessionId { get; set; }
        public string? Answer { get; set; }
        public double? DurationSeconds { get; set; }
        public bool Skipped { get; set; }

        // Các khoảng lặng (giây) trình duyệt đo được khi người dùng nói. Null nếu gõ tay.
        public List<double>? PausesSeconds { get; set; }
    }
}
