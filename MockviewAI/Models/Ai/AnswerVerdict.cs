namespace MockviewAI.Models.Ai
{
    /// <summary>
    /// [QUESTION-BANK] 
    /// Kết quả xếp loại một câu trả lời: AI trả về, sau đó AnswerVerdictValidator kiểm tra lại.
    /// </summary>
    public class AnswerVerdict
    {
        /// <summary>Mã câu trong bộ đề.</summary>
        public string QuestionId { get; set; } = string.Empty;

        /// <summary>Nhãn, xem <see cref="AnswerCategory"/>.</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>Con đường khớp: chuan / vong1... / loi1... / ngoai_bo.</summary>
        public string MatchedPath { get; set; } = string.Empty;

        /// <summary>Mã ý cốt lõi đã đạt, vd ["c1","c3"].</summary>
        public List<string> MetCorePoints { get; set; } = new();

        /// <summary>Mã ý cốt lõi còn thiếu.</summary>
        public List<string> MissingCorePoints { get; set; } = new();

        /// <summary>Nhận xét ngắn cho câu này.</summary>
        public string Feedback { get; set; } = string.Empty;
    }
}
