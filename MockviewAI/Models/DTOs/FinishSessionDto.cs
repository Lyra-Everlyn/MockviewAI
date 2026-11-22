namespace MockviewAI.Models.DTOs
{
    /// <summary>
    /// [AI-INTERVIEW] 
    /// Trình duyệt gửi lên khi kết thúc buổi (POST /Interview/Finish).
    /// </summary>
    public class FinishSessionDto
    {
        public int SessionId { get; set; }

        /// <summary>true = AI không phản hồi, người dùng chọn xem kết quả sơ bộ (không dùng AI).</summary>
        public bool Fallback { get; set; }
    }
}
