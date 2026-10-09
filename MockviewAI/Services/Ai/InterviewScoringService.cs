// [AI-MODULE] InterviewScoringService
// Điểm vào chính của module AI: gộp điểm nội dung (60%) + cách nói (40%).
// Nhóm bạn chỉ cần gọi ScoreAsync(ScoreRequest) từ phòng phỏng vấn. Điểm luôn hiển thị là 'Điểm tham khảo'.

using MockviewAI.Models.Ai;

namespace MockviewAI.Services.Ai
{
    // Combines AI content score (60%) + code-based delivery score (40%)
    public class InterviewScoringService
    {
        private const double ContentWeight = 0.6;
        private const double DeliveryWeight = 0.4;

        private readonly IAiScoringService _ai;
        private readonly DeliveryAnalyzer _delivery;

        public InterviewScoringService(IAiScoringService ai, DeliveryAnalyzer delivery)
        {
            _ai = ai;
            _delivery = delivery;
        }

        public async Task<InterviewScoreResult> ScoreAsync(ScoreRequest request, CancellationToken ct = default)
        {
            var content = await _ai.ScoreContentAsync(request, ct);
            var delivery = _delivery.Analyze(request.Answers);

            // content.Total is already 0-100 (4 criteria x 25)
            int total = (int)Math.Round(content.Total * ContentWeight + delivery.Total * DeliveryWeight);

            return new InterviewScoreResult { Content = content, Delivery = delivery, Total = total };
        }
    }
}
