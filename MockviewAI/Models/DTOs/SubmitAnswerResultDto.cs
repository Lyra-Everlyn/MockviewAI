namespace MockviewAI.Models.DTOs
{
    /// <summary>
    /// [AI-INTERVIEW] 
    /// Kết quả trả về sau khi gửi một câu: câu tiếp theo, hoặc Done = true khi đã hết.
    /// </summary>
    public class SubmitAnswerResultDto
    {
        /// <summary>true = đã hết câu, trình duyệt chuyển sang chấm điểm.</summary>
        public bool Done { get; set; }

        /// <summary>Số câu đã trả lời (không tính câu bỏ qua).</summary>
        public int AnsweredCount { get; set; }

        /// <summary>Số thứ tự câu tiếp theo; null khi Done.</summary>
        public int? NextNumber { get; set; }

        /// <summary>Nội dung câu tiếp theo; null khi Done.</summary>
        public string? NextQuestion { get; set; }

        /// <summary>[FOLLOW-UP] Câu tiếp theo là câu hỏi thêm của câu vừa trả lời (cùng số câu).</summary>
        public bool NextIsFollowUp { get; set; }

        /// <summary>Thông báo kèm theo, vd câu vừa rồi bị tính sai vì trả lời chưa phù hợp.</summary>
        public string? Notice { get; set; }

        /// <summary>[INTERVIEWER] Lời phản hồi của nhà tuyển dụng trước câu tiếp theo; null = không có.</summary>
        public string? Reaction { get; set; }
    }
}
