using MockviewAI.Models.CvQuestions;

namespace MockviewAI.Services.CvQuestions.Interfaces
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// Lớp kiểm tra bằng code, chạy trước người kiểm duyệt AI (không tốn tiền): định dạng, trùng câu, chủ đề riêng tư, căn cứ CV.
    /// </summary>
    public interface ICvQuestionRules
    {
        /// <param name="otherQuestions">Câu khác phải tránh trùng (câu CV đã nhận trước đó).</param>
        /// <returns>Lý do loại câu; null = đạt.</returns>
        string? FindProblem(CvQuestionDraft draft, CvQuestionContext ctx, IEnumerable<string> otherQuestions);
    }
}
