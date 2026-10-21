using Microsoft.Extensions.Options;
using MockviewAI.Models.Security;
using StackExchange.Redis;
using System.Net;

namespace MockviewAI.Services.Security
{
    public class LoginThrottleService : ILoginThrottleService
    {
        private readonly IDatabase _db;
        private readonly SecurityOptions _options;
        private readonly string _prefix = "MockviewAI_";

        public LoginThrottleService(IConnectionMultiplexer redis, IOptions<SecurityOptions> options)
        {
            _db = redis.GetDatabase();
            _options = options.Value;
        }

        public static string BuildKey(string email, IPAddress? ip) =>
            $"Throttle|{(email ?? string.Empty).Trim().ToLowerInvariant()}|{ip}";

        public static string BuildEmailKey(string email) =>
            $"Throttle|EmailOnly|{(email ?? string.Empty).Trim().ToLowerInvariant()}";

        public async Task<(bool LockedOut, TimeSpan Remaining)> IsLockedOutAsync(string key, string emailKey)
        {
            var now = DateTime.UtcNow;

            var ipLockedUntilStr = await _db.StringGetAsync($"{_prefix}{key}:LockedUntil");
            if (ipLockedUntilStr.HasValue && DateTime.TryParse(ipLockedUntilStr, out var ipLockedUntil) && ipLockedUntil > now)
            {
                return (true, ipLockedUntil - now);
            }

            if (!string.IsNullOrEmpty(emailKey))
            {
                var emailLockedUntilStr = await _db.StringGetAsync($"{_prefix}{emailKey}:LockedUntil");
                if (emailLockedUntilStr.HasValue && DateTime.TryParse(emailLockedUntilStr, out var emailLockedUntil) && emailLockedUntil > now)
                {
                    return (true, emailLockedUntil - now);
                }
            }

            return (false, TimeSpan.Zero);
        }

        public async Task RegisterFailureAsync(string key, string emailKey)
        {
            var failKey = $"{_prefix}{key}:Failures";
            var lockKey = $"{_prefix}{key}:LockedUntil";

            var emailFailKey = $"{_prefix}{emailKey}:Failures";
            var emailLockKey = $"{_prefix}{emailKey}:LockedUntil";

            var window = TimeSpan.FromMinutes(_options.LockoutMinutes);

            var batch = _db.CreateBatch();
            var ipIncrTask = batch.StringIncrementAsync(failKey);
            var emailIncrTask = batch.StringIncrementAsync(emailFailKey);
            batch.Execute();

            await Task.WhenAll(ipIncrTask, emailIncrTask);
            long ipFailures = await ipIncrTask;
            long emailFailures = await emailIncrTask;

            if (ipFailures == 1) await _db.KeyExpireAsync(failKey, window);
            if (ipFailures >= _options.MaxFailedLogins)
            {
                var lockedUntil = DateTime.UtcNow.Add(window);
                await _db.StringSetAsync(lockKey, lockedUntil.ToString("O"), window);
                await _db.KeyDeleteAsync(failKey);
            }

            if (emailFailures == 1) await _db.KeyExpireAsync(emailFailKey, window);
            if (emailFailures >= _options.MaxFailedLogins * 2)
            {
                var lockedUntil = DateTime.UtcNow.Add(window);
                await _db.StringSetAsync(emailLockKey, lockedUntil.ToString("O"), window);
                await _db.KeyDeleteAsync(emailFailKey);
            }
        }

        public async Task ResetAsync(string key, string emailKey)
        {
            var batch = _db.CreateBatch();
            var t1 = batch.KeyDeleteAsync($"{_prefix}{key}:Failures");
            var t2 = batch.KeyDeleteAsync($"{_prefix}{key}:LockedUntil");
            var t3 = batch.KeyDeleteAsync($"{_prefix}{emailKey}:Failures");
            var t4 = batch.KeyDeleteAsync($"{_prefix}{emailKey}:LockedUntil");
            batch.Execute();
            await Task.WhenAll(t1, t2, t3, t4);
        }
    }
}