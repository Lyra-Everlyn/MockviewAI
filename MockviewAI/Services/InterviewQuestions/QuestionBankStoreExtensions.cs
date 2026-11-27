using MockviewAI.Models.InterviewQuestions;
using MockviewAI.Services.InterviewQuestions.Interfaces;

namespace MockviewAI.Services.InterviewQuestions
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// Tìm câu hỏi theo mã: trước hết trong câu AI soạn theo CV của buổi (cv-...), sau đó tới bộ đề.
    /// Danh sách câu CV được tải một lần mỗi request rồi truyền vào, nên bộ đề vẫn là Singleton như cũ.
    /// </summary>
    public static class QuestionBankStoreExtensions
    {
        /// <summary>Tiền tố mã câu hỏi theo CV: cv-{sessionId}-{n}.</summary>
        public const string CvIdPrefix = "cv-";

        public static bool IsCvQuestionId(string? questionId) =>
            questionId != null && questionId.StartsWith(CvIdPrefix, StringComparison.Ordinal);

        /// <param name="cvQuestions">Câu CV của buổi; null / rỗng = chỉ tìm trong bộ đề (hành vi cũ).</param>
        public static BankQuestion? Find(this IQuestionBankStore bank, IReadOnlyList<BankQuestion>? cvQuestions, string? questionId)
        {
            if (questionId == null) return null;
            if (cvQuestions != null && IsCvQuestionId(questionId))
                return cvQuestions.FirstOrDefault(q => q.Id == questionId);
            return bank.Find(questionId);
        }
    }
}
