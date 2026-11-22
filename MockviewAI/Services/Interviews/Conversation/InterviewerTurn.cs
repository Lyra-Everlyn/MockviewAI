namespace MockviewAI.Services.Interviews.Conversation
{
    /// <summary>
    /// [INTERVIEWER] Dữ liệu cho một lượt phản hồi của nhà tuyển dụng sau câu trả lời.
    /// </summary>
    public class InterviewerTurnRequest
    {
        public string InterviewerName { get; init; } = string.Empty;
        public string InterviewerRole { get; init; } = string.Empty;
        public string Position { get; init; } = string.Empty;
        public string Question { get; init; } = string.Empty;
        public string Answer { get; init; } = string.Empty;

        /// <summary>Được phép hỏi thêm một câu cho câu này không (còn lượt, câu chính thuộc bộ đề).</summary>
        public bool AllowFollowUp { get; init; }

        /// <summary>Câu hỏi thêm theo ý cốt lõi còn thiếu (bộ đề); null = câu trả lời đã đủ ý chính.</summary>
        public string? MissingPointQuestion { get; init; }

        /// <summary>Tóm tắt CV ứng viên; rỗng = buổi không dùng CV.</summary>
        public string CvContext { get; init; } = string.Empty;
    }

    /// <summary>
    /// [INTERVIEWER] Kết quả: lời phản hồi ngắn và (tuỳ chọn) câu hỏi đào sâu.
    /// </summary>
    /// <param name="Reaction">1–2 câu ghi nhận, không chấm điểm, không đặt câu hỏi.</param>
    /// <param name="FollowUp">Câu hỏi đào sâu; null = sang câu tiếp theo.</param>
    /// <param name="FromAi">false = phản hồi có sẵn (AI tắt, lỗi hoặc quá giờ).</param>
    public record InterviewerTurn(string Reaction, string? FollowUp, bool FromAi);
}
