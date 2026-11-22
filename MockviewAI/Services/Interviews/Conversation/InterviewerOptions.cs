namespace MockviewAI.Services.Interviews.Conversation
{
    /// <summary>
    /// [INTERVIEWER] 
    /// Cấu hình nhà tuyển dụng trò chuyện (phản hồi + câu hỏi đào sâu sau mỗi câu trả lời).
    /// Mục "Interviewer", biến môi trường Interviewer__*.
    /// </summary>
    public class InterviewerOptions
    {
        public const string SectionName = "Interviewer";

        /// <summary>false = chỉ dùng câu chuyển tiếp có sẵn và câu hỏi thêm theo bộ đề (không gọi AI).</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Bản lite cho nhanh (khoảng 1 giây). Trống = Gemini:Model.</summary>
        public string Model { get; set; } = "gemini-3.5-flash-lite";

        /// <summary>Mức suy nghĩ gửi kèm (minimal = nhanh nhất); model không nhận thì gửi lại không kèm.</summary>
        public string ThinkingLevel { get; set; } = "minimal";

        /// <summary>Quá thời gian này (giây, kẹp 2–20) thì dùng phản hồi có sẵn, người dùng không phải chờ.</summary>
        public int TimeoutSeconds { get; set; } = 8;

        /// <summary>Thời gian giữ tóm tắt CV của buổi trong Redis (giờ, kẹp 1–48).</summary>
        public int CvContextHours { get; set; } = 6;
    }
}
