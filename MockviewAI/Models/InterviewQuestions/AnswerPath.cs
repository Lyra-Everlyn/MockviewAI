namespace MockviewAI.Models.InterviewQuestions
{
    /// <summary>
    /// [QUESTION-BANK] 
    /// Một "con đường" đúng đến điểm B: đường chuẩn (dung) hoặc đường vòng (hoi_dac_biet).
    /// </summary>
    public class AnswerPath
    {
        /// <summary>Mã đường: chuan, vong1...</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>dung | hoi_dac_biet.</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>Mô tả ngắn hướng trả lời.</summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>Từ gợi ý cho AI (không phải luật cứng); bộ chấm Mock dùng để xếp loại giả.</summary>
        public List<string> Signals { get; set; } = new();

        /// <summary>Mã các ý cốt lõi mà đường này bao phủ.</summary>
        public List<string> CoversCorePoints { get; set; } = new();
    }
}
