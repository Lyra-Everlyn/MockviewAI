// [AI-INTERVIEW] Dữ liệu người dùng gửi lên / trả về dạng JSON (DTO). Mỗi file một lớp.

namespace MockviewAI.Models.DTOs
{
    // Người dùng gửi lên khi kết thúc buổi
    public class FinishSessionDto
    {
        public int SessionId { get; set; }
        public int? AnxietyAfter { get; set; }
    }
}
