// [QUESTION-BANK] Chấm SƠ BỘ một câu trả lời theo bộ đề bằng code (không gọi AI) - dùng khi AI lỗi / quá tải.

using MockviewAI.Models.Ai;
using MockviewAI.Models.InterviewQuestions;

namespace MockviewAI.Services.InterviewQuestions.Interfaces
{
    public interface IOfflineAnswerScorer
    {
        AnswerVerdict Score(BankQuestion question, string answer);

        // [FOLLOW-UP] Các ý cốt lõi câu trả lời đã chạm tới (không xét lỗi hay gặp) - dùng chọn câu hỏi thêm
        IReadOnlyList<string> FindMetCorePoints(BankQuestion question, string answer);
    }
}
