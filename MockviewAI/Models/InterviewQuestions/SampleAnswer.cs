// [QUESTION-BANK] Câu trả lời mẫu dùng để kiểm chứng: hệ thống phải xếp đúng Category và ExpectedPath.

namespace MockviewAI.Models.InterviewQuestions
{
    public class SampleAnswer
    {
        public string Category { get; set; } = string.Empty;
        public string ExpectedPath { get; set; } = string.Empty;
        // Phong cách trả lời (vd: "gọn, đủ ý", "ví von đời thường") để đo bộ chấm trên nhiều kiểu người nói
        public string Style { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }
}
