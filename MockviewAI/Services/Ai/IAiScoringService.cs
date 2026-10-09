// [AI-MODULE] IAiScoringService
// Giao diện chung cho bộ chấm NỘI DUNG câu trả lời.
// Có 2 bản: MockAiScoringService (giả) và GeminiAiScoringService (thật). Chọn bản nào do Program.cs đọc cấu hình Ai:Provider.

using MockviewAI.Models.Ai;

namespace MockviewAI.Services.Ai
{
    public interface IAiScoringService
    {
        // Scores the CONTENT of the answers (relevance, structure, specificity, clarity)
        Task<ContentScoreResult> ScoreContentAsync(ScoreRequest request, CancellationToken ct = default);
    }
}
