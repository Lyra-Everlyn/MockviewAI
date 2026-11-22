namespace MockviewAI.Models.DTOs
{
    /// <summary>
    /// [AI-INTERVIEW] 
    /// Kết quả sau khi gửi một câu: còn câu tiếp theo hay đã hết.
    /// </summary>
    public class SubmitAnswerResultDto
    {
        public bool Done { get; set; }
        public int AnsweredCount { get; set; }
        public int? NextNumber { get; set; }
        public string? NextQuestion { get; set; }

        // [FOLLOW-UP] Câu tiếp theo là câu hỏi thêm của câu vừa trả lời (cùng số câu)
        public bool NextIsFollowUp { get; set; }

        // Thông báo kèm theo (vd: câu vừa rồi được tính là sai vì trả lời chưa phù hợp)
        public string? Notice { get; set; }

        // [INTERVIEWER] Lời phản hồi của nhà tuyển dụng trước câu tiếp theo; null = không có
        public string? Reaction { get; set; }
    }
}
