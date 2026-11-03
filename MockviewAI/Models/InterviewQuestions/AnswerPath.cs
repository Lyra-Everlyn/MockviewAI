// [QUESTION-BANK] Một "con đường" đúng đến điểm B: đường chuẩn (Category = dung) hoặc đường vòng (hoi_dac_biet).

namespace MockviewAI.Models.InterviewQuestions
{
    public class AnswerPath
    {
        public string Id { get; set; } = string.Empty;          // chuan, vong1, ...
        public string Category { get; set; } = string.Empty;    // dung | hoi_dac_biet
        public string Summary { get; set; } = string.Empty;

        // Từ gợi ý cho AI (không phải luật cứng). Bộ chấm Mock dùng để xếp loại giả.
        public List<string> Signals { get; set; } = new();
        public List<string> CoversCorePoints { get; set; } = new();
    }
}
