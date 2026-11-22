using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace MockviewAI.Services.Interviews.Conversation
{
    /// <summary>
    /// [INTERVIEWER] 
    /// Lưu tóm tắt CV theo buổi trong Redis, khoá interview:cv:{sessionId}, tự hết hạn.
    /// Redis lỗi thì bỏ qua: buổi phỏng vấn vẫn chạy, chỉ không có câu hỏi theo CV.
    /// </summary>
    public class RedisCandidateCvStore : ICandidateCvStore
    {
        #region Fields
        private const string Prefix = "interview:cv:";

        private readonly IDistributedCache _cache;
        private readonly InterviewerOptions _options;
        private readonly ILogger<RedisCandidateCvStore> _logger;
        #endregion

        #region Constructor
        public RedisCandidateCvStore(IDistributedCache cache, IOptions<InterviewerOptions> options, ILogger<RedisCandidateCvStore> logger)
        {
            _cache = cache;
            _options = options.Value;
            _logger = logger;
        }
        #endregion

        #region ICandidateCvStore
        public async Task SaveAsync(int sessionId, string cvContext, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(cvContext)) return;
            try
            {
                await _cache.SetStringAsync(Prefix + sessionId, cvContext, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(Math.Clamp(_options.CvContextHours, 1, 48))
                }, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Could not save CV context for session {SessionId}", sessionId);
            }
        }

        public async Task<string> GetAsync(int sessionId, CancellationToken ct = default)
        {
            try
            {
                return await _cache.GetStringAsync(Prefix + sessionId, ct) ?? string.Empty;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Could not read CV context for session {SessionId}", sessionId);
                return string.Empty;
            }
        }
        #endregion
    }
}
