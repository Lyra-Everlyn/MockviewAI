namespace MockviewAI.Models.InterviewQuestions
{
    /// <summary>
    /// [QUESTION-BANK] 
    /// Câu trả lời mẫu để kiểm chứng: hệ thống phải xếp đúng Category và ExpectedPath.
    /// </summary>
    public class SampleAnswer
    {
        /// <summary>Nhãn mong đợi.</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>Con đường / lỗi mong đợi.</summary>
        public string ExpectedPath { get; set; } = string.Empty;

        /// <summary>Phong cách trả lời (vd "gọn, đủ ý", "ví von đời thường") để thử bộ chấm trên nhiều kiểu người nói.</summary>
        public string Style { get; set; } = string.Empty;

        public string Answer { get; set; } = string.Empty;
    }
}
