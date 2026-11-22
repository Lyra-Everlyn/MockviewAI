namespace MockviewAI.Services.Interviews.Conversation
{
    /// <summary>
    /// [INTERVIEWER] Câu chuyển tiếp có sẵn khi không gọi AI (tắt, lỗi, quá giờ, câu bị bỏ qua).
    /// </summary>
    public static class InterviewerFallback
    {
        private static readonly string[] Acknowledgements =
        {
            "Cảm ơn bạn đã chia sẻ.",
            "Mình đã ghi nhận câu trả lời của bạn.",
            "Được rồi, cảm ơn bạn.",
            "Mình hiểu rồi, cảm ơn bạn."
        };

        public const string Skipped = "Không sao, mình chuyển sang câu tiếp theo nhé.";

        public static string Acknowledge() => Acknowledgements[Random.Shared.Next(Acknowledgements.Length)];

        /// <summary>Không gọi AI: vẫn hỏi thêm theo bộ đề nếu được phép.</summary>
        public static InterviewerTurn For(InterviewerTurnRequest r) =>
            new(Acknowledge(), r.AllowFollowUp ? r.MissingPointQuestion : null, FromAi: false);
    }
}
