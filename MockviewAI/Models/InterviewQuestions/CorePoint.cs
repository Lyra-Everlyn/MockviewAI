namespace MockviewAI.Models.InterviewQuestions
{
    /// <summary>
    /// [QUESTION-BANK] 
    /// Một ý cốt lõi (một phần của "điểm B") mà câu trả lời cần chạm tới.
    /// </summary>
    public class CorePoint
    {
        /// <summary>Mã ý: c1, c2...</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Nội dung ý, hiển thị ở trang Xem lại.</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>Từ khoá dùng khi chấm sơ bộ (AI lỗi) và khi quyết định hỏi thêm.</summary>
        public List<string> Keywords { get; set; } = new();

        /// <summary>Số từ khoá khác nhau tối thiểu để ý được coi là đạt.</summary>
        public int MinHits { get; set; } = 1;

        /// <summary>[FOLLOW-UP] Câu hỏi thêm soạn sẵn khi chưa đạt ý này; gợi hướng nhưng không chứa từ khoá của ý.</summary>
        public string? FollowUp { get; set; }
    }
}
