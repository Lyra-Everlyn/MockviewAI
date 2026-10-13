// [AI-MODULE] Lớp dữ liệu vào/ra của module AI. Mỗi file một lớp.

namespace MockviewAI.Models.Ai
{
    // Final combined score shown to the user (always labelled "Điểm tham khảo")
    public class InterviewScoreResult
    {
        public ContentScoreResult Content { get; set; } = new();
        public DeliveryScoreResult Delivery { get; set; } = new();
        public int Total { get; set; }                  // round(0.6*content + 0.4*delivery)
    }
}
