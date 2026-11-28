namespace MockviewAI.Models.CvQuestions
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// Câu hỏi theo CV đã qua luật code + kiểm duyệt. Rỗng = dùng 100% câu bộ đề.
    /// </summary>
    public class CvQuestionPipelineResult
    {
        public List<CvQuestionDraft> Accepted { get; set; } = new();
        public string GeneratorModel { get; set; } = string.Empty;
        public string ReviewerModel { get; set; } = string.Empty;

        public static CvQuestionPipelineResult Empty => new();
    }
}
