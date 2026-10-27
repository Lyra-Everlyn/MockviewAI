namespace MockviewAI.Models.DTOs
{

    /// <summary>
    /// [AI-INTERVIEW]
    /// -- Người dùng gửi lên khi kết thúc buổi
    /// </summary>
    
    public class FinishSessionDto
    {
        public int SessionId { get; set; }

        // true: AI không phản hồi, người dùng chọn xem kết quả dự phòng (chỉ tính cách nói)
        public bool Fallback { get; set; }
    }
}
