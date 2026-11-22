using MockviewAI.Models.InterviewQuestions;

namespace MockviewAI.Services.InterviewQuestions.Interfaces
{
    /// <summary>
    /// [QUESTION-BANK] 
    /// Nơi lấy bộ đề có đáp án (Data/InterviewQuestions/question-bank.v1.json).
    /// </summary>
    public interface IQuestionBankStore
    {
        /// <summary>Phiên bản bộ đề, lưu vào InterviewSession.QuestionBankVersion.</summary>
        string Version { get; }

        IReadOnlyList<BankQuestion> Questions { get; }

        /// <returns>null nếu mã trống hoặc không có trong bộ đề.</returns>
        BankQuestion? Find(string? questionId);

        /// <summary>Các câu của một người phỏng vấn (linh | nam | an) cho một vị trí (developer | tester | ba).</summary>
        IReadOnlyList<BankQuestion> ForInterview(string interviewerKey, string positionKey);
    }
}
