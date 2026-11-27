using MockviewAI.Models.CvQuestions;

namespace MockviewAI.Services.CvQuestions.Interfaces
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// Người kiểm duyệt: một AI khác họ với người soạn duyệt từng câu theo 6 tiêu chí trước khi người dùng thấy.
    /// </summary>
    public interface ICvQuestionReviewer
    {
        /// <summary>Tên model kiểm duyệt (ghi vào DB).</summary>
        string Model { get; }

        /// <returns>Đúng một kết quả cho mỗi câu, theo thứ tự gửi lên; câu AI không trả lời = reject.</returns>
        /// <exception cref="MockviewAI.Services.Exceptions.CvQuestionException">AI lỗi / hết lượt: không câu nào được qua.</exception>
        Task<List<CvQuestionReview>> ReviewAsync(CvQuestionContext ctx, IReadOnlyList<CvQuestionDraft> drafts, CancellationToken ct);
    }
}
