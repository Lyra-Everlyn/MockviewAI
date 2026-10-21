namespace MockviewAI.Models.DTOs
{
    /// [AI-INTERVIEW]
    // -- Kết quả sau khi gửi một câu: còn câu tiếp theo hay đã hết
    public class SubmitAnswerResultDto
    {
        public bool Done { get; set; }
        public int AnsweredCount { get; set; }
        public int? NextNumber { get; set; }
        public string? NextQuestion { get; set; }

        // Thông báo kèm theo (vd: câu vừa rồi được tính là bỏ qua vì trả lời chưa phù hợp)
        public string? Notice { get; set; }
    }
}
