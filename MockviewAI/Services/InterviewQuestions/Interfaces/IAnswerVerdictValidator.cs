// [QUESTION-BANK] Kiểm tra lại kết quả xếp loại của AI theo bộ đề (không tin AI hoàn toàn).

using MockviewAI.Models.Ai;

namespace MockviewAI.Services.InterviewQuestions.Interfaces
{
    public interface IAnswerVerdictValidator
    {
        // askedQuestionIds: mã các câu đã gửi AI chấm. Trả về danh sách đã sửa / bỏ mục không hợp lệ.
        List<AnswerVerdict> Normalize(IEnumerable<AnswerVerdict> rawVerdicts, IReadOnlyCollection<string> askedQuestionIds);
    }
}
