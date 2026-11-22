namespace MockviewAI.Models.Entities
{
    /// <summary>
    /// [AI-INTERVIEW] 
    /// Các trạng thái của một buổi luyện (InterviewSession.Status).
    /// </summary>
    public static class SessionStatus
    {
        /// <summary>Đang làm.</summary>
        public const string InProgress = "InProgress";

        /// <summary>Đã chấm điểm.</summary>
        public const string Completed = "Completed";

        /// <summary>Dừng khi chưa trả lời câu nào, không có điểm.</summary>
        public const string Stopped = "Stopped";
    }
}
