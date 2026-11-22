using MockviewAI.Models.InterviewQuestions;

namespace MockviewAI.Services.InterviewQuestions.Interfaces
{
    /// <summary>
    /// [QUESTION-BANK] 
    /// Chọn câu hỏi cho một buổi luyện từ bộ đề.
    /// </summary>
    public interface IQuestionSelector
    {
        /// <returns>Tối đa <paramref name="count"/> câu; rỗng nếu bộ đề chưa có câu cho cặp người phỏng vấn + vị trí này.</returns>
        List<BankQuestion> Select(string interviewerKey, string positionKey, int count);
    }
}
