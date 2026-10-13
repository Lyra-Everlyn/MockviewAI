// [AI-INTERVIEW] Dữ liệu người dùng gửi lên / trả về dạng JSON (DTO). Mỗi file một lớp.

namespace MockviewAI.Models.DTOs
{
    // Kết quả sau khi gửi một câu: còn câu tiếp theo hay đã hết
    public class SubmitAnswerResultDto
    {
        public bool Done { get; set; }
        public int AnsweredCount { get; set; }
        public int? NextNumber { get; set; }
        public string? NextQuestion { get; set; }
    }
}
