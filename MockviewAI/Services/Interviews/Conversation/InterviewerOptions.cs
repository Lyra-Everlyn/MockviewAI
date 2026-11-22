namespace MockviewAI.Services.Interviews.Conversation
{
    /// <summary>
    /// [INTERVIEWER] Cấu hình nhà tuyển dụng trò chuyện (phản hồi + câu hỏi đào sâu sau mỗi câu trả lời). Section "Interviewer".
    /// </summary>
    public class InterviewerOptions
    {
        public const string SectionName = "Interviewer";

        /// <summary>false = chỉ dùng câu chuyển tiếp có sẵn và câu hỏi thêm theo bộ đề (không gọi AI).</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Bản lite cho nhanh (~1 giây). Trống = Gemini:Model.</summary>
        public string Model { get; set; } = "gemini-3.5-flash-lite";

        public string ThinkingLevel { get; set; } = "minimal";

        /// <summary>Quá thời gian này thì dùng phản hồi có sẵn, người dùng không phải chờ.</summary>
        public int TimeoutSeconds { get; set; } = 8;

        /// <summary>Thời gian giữ tóm tắt CV của buổi trong Redis.</summary>
        public int CvContextHours { get; set; } = 6;
    }
}
