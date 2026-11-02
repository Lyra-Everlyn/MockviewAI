// [AI-MODULE] Lớp dữ liệu vào/ra của module AI. Mỗi file một lớp.

namespace MockviewAI.Models.Ai
{
    // Content score from the AI: 4 criteria x 25 points = 100
    public class ContentScoreResult
    {
        public int Relevance { get; set; }     // Trả lời đúng câu hỏi
        public int Structure { get; set; }     // Bố cục, mạch lạc
        public int Specificity { get; set; }   // Ví dụ, số liệu cụ thể
        public int Clarity { get; set; }       // Rõ ràng, dễ hiểu

        // Tỉ lệ câu hợp lệ / tổng câu đã trả lời (câu không có ý nghĩa = 0 điểm). Mặc định 1 = không ảnh hưởng.
        public double ValidAnswerRatio { get; set; } = 1.0;
        public int Total => (int)Math.Round((Relevance + Structure + Specificity + Clarity) * ValidAnswerRatio);

        public string Summary { get; set; } = string.Empty;
        public List<string> Strengths { get; set; } = new();
        public List<string> Improvements { get; set; } = new();

        // [QUESTION-BANK] Xếp loại từng câu (chỉ có với câu thuộc bộ đề). Đã qua AnswerVerdictValidator trước khi lưu.
        public List<AnswerVerdict> Verdicts { get; set; } = new();
    }
}
