// [AI-MODULE] IDeliveryAnalyzer
// Giao diện bộ chấm CÁCH NÓI (tốc độ, từ đệm, độ dài, khoảng lặng). Bản thật là DeliveryAnalyzer.

using MockviewAI.Models.Ai;

namespace MockviewAI.Services.Ai.Interfaces
{
    public interface IDeliveryAnalyzer
    {
        DeliveryScoreResult Analyze(IReadOnlyList<InterviewAnswer> answers);
    }
}
