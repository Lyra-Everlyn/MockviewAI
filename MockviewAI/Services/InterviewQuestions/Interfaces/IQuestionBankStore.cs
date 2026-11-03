// [QUESTION-BANK] Nơi lấy bộ đề có đáp án (đọc từ Data/InterviewQuestions/question-bank.v1.json).

using MockviewAI.Models.InterviewQuestions;

namespace MockviewAI.Services.InterviewQuestions.Interfaces
{
    public interface IQuestionBankStore
    {
        string Version { get; }
        IReadOnlyList<BankQuestion> Questions { get; }

        // null nếu không có câu này trong bộ đề
        BankQuestion? Find(string? questionId);

        // Các câu của một người phỏng vấn (linh | nam | an) cho một vị trí (developer | tester | ba)
        IReadOnlyList<BankQuestion> ForInterview(string interviewerKey, string positionKey);
    }
}
