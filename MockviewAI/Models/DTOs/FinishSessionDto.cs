namespace MockviewAI.Models.DTOs
{

    /// [AI-INTERVIEW]
    // -- Người dùng gửi lên khi kết thúc buổi
    public class FinishSessionDto
    {
        public int SessionId { get; set; }
        public int? AnxietyAfter { get; set; }
    }
}
