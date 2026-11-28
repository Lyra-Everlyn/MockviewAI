using MockviewAI.Models.CvQuestions;

namespace MockviewAI.Services.CvQuestions.Interfaces
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// Ghép các bước: soạn -> luật code -> kiểm duyệt -> sửa 1 lần. Không bao giờ ném lỗi: lỗi / hết giờ thì trả rỗng (dùng bộ đề).
    /// </summary>
    public interface ICvQuestionPipeline
    {
        Task<CvQuestionPipelineResult> RunAsync(CvQuestionContext ctx, CancellationToken ct = default);
    }
}
