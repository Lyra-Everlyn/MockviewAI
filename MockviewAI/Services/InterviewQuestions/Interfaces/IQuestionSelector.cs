// [QUESTION-BANK] Chọn câu hỏi cho một buổi luyện từ bộ đề.

using MockviewAI.Models.InterviewQuestions;

namespace MockviewAI.Services.InterviewQuestions.Interfaces
{
    public interface IQuestionSelector
    {
        // Trả về tối đa `count` câu của người phỏng vấn + vị trí; rỗng nếu bộ đề chưa có câu cho cặp này
        List<BankQuestion> Select(string interviewerKey, string positionKey, int count);
    }
}
