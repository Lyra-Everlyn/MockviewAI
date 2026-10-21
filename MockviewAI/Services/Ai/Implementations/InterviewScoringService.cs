// [AI-MODULE] InterviewScoringService
// Điểm vào chính của module AI: gộp điểm nội dung (60%) + cách nói (40%).
// Nhóm bạn chỉ cần gọi ScoreAsync(ScoreRequest) từ phòng phỏng vấn. Điểm luôn hiển thị là 'Điểm tham khảo'.
// Khi AI lỗi: gọi ScoreDeliveryOnly (chấm dự phòng, chỉ tính cách nói).

using MockviewAI.Models.Ai;
using MockviewAI.Services.Ai.Interfaces;

namespace MockviewAI.Services.Ai.Implementations
{
    // Combines AI content score (60%) + code-based delivery score (40%)
    public class InterviewScoringService : IInterviewScoringService
    {
        private const double ContentWeight = 0.6;
        private const double DeliveryWeight = 0.4;

        private readonly IAiScoringService _ai;
        private readonly IDeliveryAnalyzer _delivery;
        private readonly IAnxietyEstimator _anxiety;

        public InterviewScoringService(IAiScoringService ai, IDeliveryAnalyzer delivery, IAnxietyEstimator anxiety)
        {
            _ai = ai;
            _delivery = delivery;
            _anxiety = anxiety;
        }

        public async Task<InterviewScoreResult> ScoreAsync(ScoreRequest request, CancellationToken ct = default)
        {
            var content = await _ai.ScoreContentAsync(request, ct);
            var delivery = _delivery.Analyze(request.Answers);

            // content.Total is already 0-100 (4 criteria x 25)
            int total = (int)Math.Round(content.Total * ContentWeight + delivery.Total * DeliveryWeight);

            return Build(request, content, delivery, total, contentAvailable: true);
        }

        public InterviewScoreResult ScoreDeliveryOnly(ScoreRequest request)
        {
            var delivery = _delivery.Analyze(request.Answers);
            var content = new ContentScoreResult
            {
                Summary = "AI đang bận nên chưa nhận xét được nội dung. Điểm này chỉ tính phần cách nói. Hãy luyện lại sau để có nhận xét đầy đủ."
            };
            return Build(request, content, delivery, delivery.Total, contentAvailable: false);
        }

        private InterviewScoreResult Build(ScoreRequest request, ContentScoreResult content, DeliveryScoreResult delivery, int total, bool contentAvailable)
        {
            var (level, reasons) = _anxiety.Estimate(delivery, request.QuestionCount, request.SkippedCount);
            return new InterviewScoreResult
            {
                Content = content,
                Delivery = delivery,
                Total = total,
                ContentAvailable = contentAvailable,
                AnxietyIndicator = level,
                AnxietyReasons = reasons
            };
        }
    }
}
