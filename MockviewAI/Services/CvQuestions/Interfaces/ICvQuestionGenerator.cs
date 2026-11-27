using MockviewAI.Models.CvQuestions;

namespace MockviewAI.Services.CvQuestions.Interfaces
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// Người soạn: AI đọc tóm tắt CV và soạn câu hỏi chính cùng định dạng bộ đề (BankQuestion).
    /// </summary>
    public interface ICvQuestionGenerator
    {
        /// <param name="count">Số câu cần soạn.</param>
        /// <param name="fixNotes">Vòng sửa: mỗi mục là câu cũ + lý do cần sửa; null = soạn mới.</param>
        /// <exception cref="MockviewAI.Services.Exceptions.CvQuestionException">AI lỗi, hết lượt, trả dữ liệu không đọc được.</exception>
        Task<CvGenerationResult> GenerateAsync(CvQuestionContext ctx, int count, IReadOnlyList<string>? fixNotes, CancellationToken ct);
    }
}
