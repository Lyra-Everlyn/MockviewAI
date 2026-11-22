using MockviewAI.Models.Ai;

namespace MockviewAI.Services.InterviewQuestions.Interfaces
{
    /// <summary>
    /// [QUESTION-BANK] 
    /// Kiểm tra lại kết quả xếp loại của AI theo bộ đề (không tin AI hoàn toàn).
    /// </summary>
    public interface IAnswerVerdictValidator
    {
        /// <param name="rawVerdicts">Xếp loại AI trả về.</param>
        /// <param name="askedQuestionIds">Mã các câu đã gửi AI chấm.</param>
        /// <returns>Danh sách đã sửa nhãn và bỏ mục không hợp lệ.</returns>
        List<AnswerVerdict> Normalize(IEnumerable<AnswerVerdict> rawVerdicts, IReadOnlyCollection<string> askedQuestionIds);
    }
}
