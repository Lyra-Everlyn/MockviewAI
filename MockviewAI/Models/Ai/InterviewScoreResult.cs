using System.Runtime.InteropServices;

namespace MockviewAI.Models.Ai
{
    /// <summary>
    /// [AI-MODULE] 
    /// -- Lớp dữ liệu vào/ra của module AI.Mỗi file một lớp.
    /// -- Final combined score shown to the user (always labelled "Điểm tham khảo")
    /// </summary>
    
    public class InterviewScoreResult
    {
        public ContentScoreResult Content { get; set; } = new();
        public DeliveryScoreResult Delivery { get; set; } = new();
        public int Total { get; set; }                  // round(0.6*content + 0.4*delivery); chấm dự phòng = điểm cách nói

        // false khi AI không phản hồi và hệ thống chấm dự phòng (chỉ tính cách nói)
        public bool ContentAvailable { get; set; } = true;

        // Dấu hiệu lo lắng ước tính 1-10 (0 = chưa đủ dữ liệu) và các lý do đi kèm
        public int AnxietyIndicator { get; set; }
        public List<string> AnxietyReasons { get; set; } = new();
    }
}
