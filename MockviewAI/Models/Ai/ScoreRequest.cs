// [AI-MODULE] Lớp dữ liệu vào/ra của module AI. Mỗi file một lớp.

namespace MockviewAI.Models.Ai
{
    // Input for scoring a whole practice session
    public class ScoreRequest
    {
        public string Position { get; set; } = string.Empty;   // e.g. "Lập trình viên"
        public List<InterviewAnswer> Answers { get; set; } = new();
    }
}
