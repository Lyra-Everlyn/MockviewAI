// [QUESTION-BANK] Chấm SƠ BỘ một câu trả lời theo bộ đề bằng code (không gọi AI) - dùng khi AI lỗi / quá tải.

using MockviewAI.Models.Ai;
using MockviewAI.Models.InterviewQuestions;

namespace MockviewAI.Services.InterviewQuestions.Interfaces
{
    public interface IOfflineAnswerScorer
    {
        AnswerVerdict Score(BankQuestion question, string answer);
    }
}
