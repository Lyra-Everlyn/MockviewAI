namespace MockviewAI.Models.Entities
{
    /// <summary>
    /// [AI-INTERVIEW]
    /// -- Các trạng thái của một buổi luyện
    /// </summary>
    public static class SessionStatus
    {
        public const string InProgress = "InProgress";   // đang làm
        public const string Completed = "Completed";     // đã chấm điểm
        public const string Stopped = "Stopped";         // dừng khi chưa trả lời câu nào
    }
}
