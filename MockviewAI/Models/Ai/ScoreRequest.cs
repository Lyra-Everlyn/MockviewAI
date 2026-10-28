namespace MockviewAI.Models.Ai
{
    /// <summary>
    /// [AI-MODULE] 
    /// -- Lớp dữ liệu vào/ra của module AI
    /// -- Input for scoring a whole practice session
    /// </summary>
    
    public class ScoreRequest
    {
        public string Position { get; set; } = string.Empty;   // e.g. "Lập trình viên"
        public List<InterviewAnswer> Answers { get; set; } = new();

        // Dùng để ước tính dấu hiệu lo lắng: tổng số câu của buổi và số câu bị bỏ qua
        public int QuestionCount { get; set; }
        public int SkippedCount { get; set; }
    }
}
