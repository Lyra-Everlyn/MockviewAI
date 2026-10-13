// [AI-MODULE] IInterviewScoringService
// Điểm vào của module AI: nhận ScoreRequest, trả InterviewScoreResult (nội dung + cách nói + tổng).

using MockviewAI.Models.Ai;

namespace MockviewAI.Services.Ai.Interfaces
{
    public interface IInterviewScoringService
    {
        Task<InterviewScoreResult> ScoreAsync(ScoreRequest request, CancellationToken ct = default);
    }
}
