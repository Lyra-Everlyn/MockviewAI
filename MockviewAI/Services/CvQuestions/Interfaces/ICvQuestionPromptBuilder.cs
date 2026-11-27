using MockviewAI.Models.CvQuestions;

namespace MockviewAI.Services.CvQuestions.Interfaces
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// Soạn prompt cho người soạn (Gemini) và người kiểm duyệt (Groq). Muốn đổi cách AI soạn / duyệt: chỉ sửa phần cài đặt.
    /// </summary>
    public interface ICvQuestionPromptBuilder
    {
        string GeneratorSystemPrompt { get; }
        string BuildGeneratorPrompt(CvQuestionContext ctx, int count, IReadOnlyList<string>? fixNotes);

        string ReviewerSystemPrompt { get; }
        string BuildReviewerPrompt(CvQuestionContext ctx, IReadOnlyList<CvQuestionDraft> drafts);
    }
}
