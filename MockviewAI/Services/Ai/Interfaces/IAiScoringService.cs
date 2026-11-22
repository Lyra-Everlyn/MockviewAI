using MockviewAI.Models.Ai;

namespace MockviewAI.Services.Ai.Interfaces
{
    /// <summary>
    /// [AI-SCORING] 
    /// Bộ chấm NỘI DUNG câu trả lời. Hai bản: GeminiAiScoringService (thật) và MockAiScoringService (giả, chỉ Development).
    /// AddAiScoring chọn bản nào theo cấu hình Ai:Provider.
    /// </summary>
    public interface IAiScoringService
    {
        /// <summary>Chấm 4 tiêu chí (relevance, structure, specificity, clarity) và xếp loại từng câu thuộc bộ đề.</summary>
        /// <exception cref="Exceptions.AiScoringException">AI lỗi, quá tải hoặc cấu hình sai.</exception>
        Task<ContentScoreResult> ScoreContentAsync(ScoreRequest request, CancellationToken ct = default);
    }
}
