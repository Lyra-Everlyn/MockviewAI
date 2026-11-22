using System.Net;
using Microsoft.Extensions.Options;
using MockviewAI.Models.Security;
using StackExchange.Redis;

namespace MockviewAI.Services.Security
{
    /// <summary>
    /// [SECURITY] 
    /// Đếm lần đăng nhập sai trong Redis và khoá tạm thời.
    /// Khoá cặp email + IP sau MaxFailedLogins lần sai; khoá riêng email sau gấp đôi số đó (chống dò từ nhiều IP).
    /// Khoá Redis: MockviewAI_Throttle|...:Failures (bộ đếm) và :Locked (đang khoá, TTL = thời gian còn lại).
    /// </summary>
    public class LoginThrottleService : ILoginThrottleService
    {
        #region Fields
        private const string Prefix = "MockviewAI_";

        private readonly IDatabase _db;
        private readonly SecurityOptions _options;
        #endregion

        #region Constructor
        public LoginThrottleService(IConnectionMultiplexer redis, IOptions<SecurityOptions> options)
        {
            _db = redis.GetDatabase();
            _options = options.Value;
        }
        #endregion

        #region Dựng khoá
        /// <summary>Khoá theo cặp email + IP.</summary>
        public static string BuildKey(string email, IPAddress? ip) =>
            $"Throttle|{(email ?? string.Empty).Trim().ToLowerInvariant()}|{ip}";

        /// <summary>Khoá chỉ theo email.</summary>
        public static string BuildEmailKey(string email) =>
            $"Throttle|EmailOnly|{(email ?? string.Empty).Trim().ToLowerInvariant()}";
        #endregion

        #region ILoginThrottleService
        public async Task<(bool LockedOut, TimeSpan Remaining)> IsLockedOutAsync(string key, string emailKey)
        {
            foreach (var k in new[] { key, emailKey })
            {
                if (string.IsNullOrEmpty(k)) continue;
                var ttl = await _db.KeyTimeToLiveAsync(Prefix + k + ":Locked");
                if (ttl is { } remaining && remaining > TimeSpan.Zero) return (true, remaining);
            }
            return (false, TimeSpan.Zero);
        }

        public async Task RegisterFailureAsync(string key, string emailKey)
        {
            var window = TimeSpan.FromMinutes(Math.Max(1, _options.LockoutMinutes));
            int max = Math.Max(1, _options.MaxFailedLogins);

            await CountAsync(key, max, window);
            await CountAsync(emailKey, max * 2, window);
        }

        public async Task ResetAsync(string key, string emailKey)
        {
            await _db.KeyDeleteAsync(new RedisKey[]
            {
                Prefix + key + ":Failures", Prefix + key + ":Locked",
                Prefix + emailKey + ":Failures", Prefix + emailKey + ":Locked"
            });
        }
        #endregion

        #region Helpers
        /// <summary>Tăng bộ đếm (hết hạn sau <paramref name="window"/> tính từ lần sai đầu); chạm ngưỡng thì đặt khoá và xoá bộ đếm.</summary>
        private async Task CountAsync(string key, int limit, TimeSpan window)
        {
            if (string.IsNullOrEmpty(key)) return;

            string failKey = Prefix + key + ":Failures";
            long failures = await _db.StringIncrementAsync(failKey);
            if (failures == 1) await _db.KeyExpireAsync(failKey, window);

            if (failures < limit) return;
            await _db.StringSetAsync(Prefix + key + ":Locked", "1", window);
            await _db.KeyDeleteAsync(failKey);
        }
        #endregion
    }
}
