using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using MockviewAI.Models.DTOs;

namespace MockviewAI.Services.CvParser
{
    /// <summary>
    /// [USER CV] Lưu bản chờ xác nhận trong Redis, khoá cv:pending:{email}, tự hết hạn.
    /// </summary>
    public class RedisCvStore : ICvStore
    {
        #region Fields
        private const string PendingPrefix = "cv:pending:";

        private readonly IDistributedCache _cache;
        private readonly CvOptions _options;
        #endregion

        #region Constructor
        public RedisCvStore(IDistributedCache cache, IOptions<CvOptions> options)
        {
            _cache = cache;
            _options = options.Value;
        }
        #endregion

        #region ICvStore
        public Task SavePendingAsync(string email, CvParsedDataDto data, CancellationToken ct = default) =>
            _cache.SetStringAsync(Key(email), JsonSerializer.Serialize(data),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(Math.Max(5, _options.PendingMinutes))
                }, ct);

        public async Task<CvParsedDataDto?> GetPendingAsync(string email, CancellationToken ct = default)
        {
            var json = await _cache.GetStringAsync(Key(email), ct);
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                return JsonSerializer.Deserialize<CvParsedDataDto>(json);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public Task RemovePendingAsync(string email, CancellationToken ct = default) =>
            _cache.RemoveAsync(Key(email), ct);
        #endregion

        #region Helpers
        private static string Key(string email) => PendingPrefix + (email ?? string.Empty).Trim().ToLowerInvariant();
        #endregion
    }
}
