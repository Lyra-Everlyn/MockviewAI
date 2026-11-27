using MockviewAI.Models.InterviewQuestions;

namespace MockviewAI.Models.CvQuestions
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// Một câu hỏi AI soạn theo CV: cùng định dạng bộ đề (BankQuestion) + đoạn trích CV làm căn cứ.
    /// </summary>
    public class CvQuestionDraft
    {
        public BankQuestion Question { get; set; } = new();

        /// <summary>Đoạn trích ngắn từ CV mà câu hỏi dựa vào.</summary>
        public string CvEvidence { get; set; } = string.Empty;

        /// <summary>pass | fixed sau khi được duyệt; rỗng = chưa duyệt.</summary>
        public string ReviewVerdict { get; set; } = string.Empty;

        /// <summary>Lý do người kiểm duyệt yêu cầu sửa (nếu có).</summary>
        public string? ReviewNotes { get; set; }
    }
}
