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

        /// <summary>[IT-ROLES] cv = theo CV; role = câu kỹ thuật chuẩn theo vị trí (không cần căn cứ CV). Xem CvQuestionKinds.</summary>
        public string Kind { get; set; } = CvQuestionKinds.Cv;

        /// <summary>Đoạn trích ngắn từ CV mà câu hỏi dựa vào (câu theo vị trí để trống).</summary>
        public string CvEvidence { get; set; } = string.Empty;

        /// <summary>pass | fixed sau khi được duyệt; rỗng = chưa duyệt.</summary>
        public string ReviewVerdict { get; set; } = string.Empty;

        /// <summary>Lý do người kiểm duyệt yêu cầu sửa (nếu có).</summary>
        public string? ReviewNotes { get; set; }
    }
}
