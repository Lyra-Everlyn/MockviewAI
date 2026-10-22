namespace MockviewAI.Models.DTOs
{

    /// [AI-INTERVIEW]
    // -- Người dùng gửi lên khi kết thúc buổi
    public class FinishSessionDto
    {
        public int SessionId { get; set; }

        // true: AI không phản hồi, người dùng chọn xem kết quả dự phòng (chỉ tính cách nói)
        public bool Fallback { get; set; }
    }
}
