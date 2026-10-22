// [AI-INTERVIEW] Các trạng thái của một buổi luyện. Dùng hằng số thay vì gõ chuỗi tay ở nhiều nơi (tránh gõ sai).

namespace MockviewAI.Models.Entities
{
    public static class SessionStatus
    {
        public const string InProgress = "InProgress";   // đang làm
        public const string Completed = "Completed";     // đã chấm điểm
        public const string Stopped = "Stopped";         // dừng khi chưa trả lời câu nào
    }
}
