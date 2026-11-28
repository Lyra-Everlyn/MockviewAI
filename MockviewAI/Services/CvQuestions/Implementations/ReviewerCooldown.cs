using System.Collections.Concurrent;
using MockviewAI.Services.CvQuestions.Interfaces;

namespace MockviewAI.Services.CvQuestions.Implementations
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// Lưu trong bộ nhớ (mỗi tiến trình một bản): tên model -> thời điểm hết nghỉ. Khởi động lại app thì quên, không sao.
    /// </summary>
    public class ReviewerCooldown : IReviewerCooldown
    {
        #region Fields
        private readonly ConcurrentDictionary<string, DateTimeOffset> _until = new(StringComparer.OrdinalIgnoreCase);
        private readonly TimeProvider _clock;
        #endregion

        #region Constructor
        public ReviewerCooldown(TimeProvider clock)
        {
            _clock = clock;
        }
        #endregion

        #region IReviewerCooldown
        public bool IsCooling(string model) =>
            _until.TryGetValue(model, out var until) && _clock.GetUtcNow() < until;

        public void Trip(string model, TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero) return;
            var until = _clock.GetUtcNow().Add(duration);
            _until.AddOrUpdate(model, until, (_, old) => old > until ? old : until);
        }
        #endregion
    }
}
