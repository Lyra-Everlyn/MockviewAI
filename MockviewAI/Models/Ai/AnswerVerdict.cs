// [QUESTION-BANK] Kết quả xếp loại 1 câu trả lời (AI trả về, sau đó AnswerVerdictValidator kiểm tra lại).

namespace MockviewAI.Models.Ai
{
    public class AnswerVerdict
    {
        public string QuestionId { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;       // AnswerCategory.*
        public string MatchedPath { get; set; } = string.Empty;    // chuan / vong1 / loi1 / ngoai_bo
        public List<string> MetCorePoints { get; set; } = new();   // vd ["c1","c3"]
        public List<string> MissingCorePoints { get; set; } = new();
        public string Feedback { get; set; } = string.Empty;
    }
}
