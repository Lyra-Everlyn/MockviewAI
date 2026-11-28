using MockviewAI.Models.InterviewQuestions;
using MockviewAI.Services.InterviewQuestions.Interfaces;

namespace MockviewAI.Services.InterviewQuestions
{
    /// <summary>
    /// [CV-QUESTIONS] [IT-ROLES]
    /// Tìm câu hỏi theo mã: trước hết trong câu AI soạn của buổi (cv-... theo CV, ai-... theo vị trí), sau đó tới bộ đề.
    /// Danh sách câu AI soạn được tải một lần mỗi request rồi truyền vào, nên bộ đề vẫn là Singleton như cũ.
    /// </summary>
    public static class QuestionBankStoreExtensions
    {
        /// <summary>Tiền tố mã câu hỏi theo CV: cv-{sessionId}-{n}.</summary>
        public const string CvIdPrefix = "cv-";

        /// <summary>Tiền tố mã câu kỹ thuật AI soạn theo vị trí (vị trí chưa có bộ đề): ai-{sessionId}-{n}.</summary>
        public const string RoleIdPrefix = "ai-";

        public static bool IsCvQuestionId(string? questionId) =>
            questionId != null && questionId.StartsWith(CvIdPrefix, StringComparison.Ordinal);

        public static bool IsRoleQuestionId(string? questionId) =>
            questionId != null && questionId.StartsWith(RoleIdPrefix, StringComparison.Ordinal);

        /// <summary>Câu do AI soạn (lưu ở bảng CvGeneratedQuestions), không thuộc bộ đề JSON.</summary>
        public static bool IsGeneratedQuestionId(string? questionId) => IsCvQuestionId(questionId) || IsRoleQuestionId(questionId);

        /// <param name="generated">Câu AI soạn của buổi; null / rỗng = chỉ tìm trong bộ đề (hành vi cũ).</param>
        public static BankQuestion? Find(this IQuestionBankStore bank, IReadOnlyList<BankQuestion>? generated, string? questionId)
        {
            if (questionId == null) return null;
            if (generated != null && IsGeneratedQuestionId(questionId))
                return generated.FirstOrDefault(q => q.Id == questionId);
            return bank.Find(questionId);
        }

        /// <summary>
        /// [IT-ROLES] true = Nam (kỹ thuật) không có câu bộ đề hợp với vị trí này: vị trí gốc không hợp (HasTechBank = false)
        /// và file question-bank.it-roles chưa có câu riêng -> phải nhờ AI soạn câu kỹ thuật theo vị trí.
        /// </summary>
        public static bool NeedsAiTechQuestions(this IQuestionBankStore bank, ItRole role) =>
            !role.HasTechBank && !bank.ForInterview("nam", role.Key).Any(q => !q.IsIntro);
    }
}
