using MockviewAI.Models.Ai;
using MockviewAI.Models.InterviewQuestions;

namespace MockviewAI.Services.InterviewQuestions.Interfaces
{
    /// <summary>
    /// [QUESTION-BANK] 
    /// Chấm sơ bộ một câu trả lời theo bộ đề bằng code, không gọi AI (dùng khi AI lỗi / quá tải và để chọn câu hỏi thêm).
    /// </summary>
    public interface IOfflineAnswerScorer
    {
        /// <summary>Xếp loại 4 nhãn kèm ý đạt / thiếu.</summary>
        AnswerVerdict Score(BankQuestion question, string answer);

        /// <summary>[FOLLOW-UP] Mã các ý cốt lõi câu trả lời đã chạm tới, không xét lỗi hay gặp.</summary>
        IReadOnlyList<string> FindMetCorePoints(BankQuestion question, string answer);
    }
}
