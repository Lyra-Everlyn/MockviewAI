using Microsoft.Extensions.Caching.Distributed;
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

        // --- Key kết hợp (Email + IP) -> Chống dò nhiều pass trên 1 IP
        public static string BuildKey(string email, IPAddress? ip) =>
            $"Throttle|{(email ?? string.Empty).Trim().ToLowerInvariant()}|{ip}";

        // --- Key chỉ có Email -> Chống xoay IP dò 1 tài khoản
        public static string BuildEmailKey(string email) =>
            $"Throttle|EmailOnly|{(email ?? string.Empty).Trim().ToLowerInvariant()}";

        public bool IsLockedOut(string key, string emailKey, out TimeSpan remaining)
        {
            remaining = TimeSpan.Zero;

            // 1. Kiểm tra khóa IP
            var ipLockedUntilStr = _db.StringGet($"{_prefix}{key}:LockedUntil");
            if (ipLockedUntilStr.HasValue && DateTime.TryParse(ipLockedUntilStr, out var ipLockedUntil))
            {
                var now = DateTime.UtcNow;
                if (ipLockedUntil > now)
                {
                    remaining = ipLockedUntil - now;
                    return true;
                }
            }

            // 2. Kiểm tra khóa Email (Mới bổ sung)
            var emailLockedUntilStr = _db.StringGet($"{_prefix}{emailKey}:LockedUntil");
            if (emailLockedUntilStr.HasValue && DateTime.TryParse(emailLockedUntilStr, out var emailLockedUntil))
            {
                var now = DateTime.UtcNow;
                if (emailLockedUntil > now)
                {
                    remaining = emailLockedUntil - now;
                    return true;
                }
            }

            return false;
        }

        public bool IsLockedOut(string key, out TimeSpan remaining) => IsLockedOut(key, "", out remaining);

        public void RegisterFailure(string key, string emailKey)
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

            long ipFailures = ipIncrTask.Result;
            long emailFailures = emailIncrTask.Result;

            if (ipFailures == 1) _db.KeyExpire(failKey, window);
            if (ipFailures >= _options.MaxFailedLogins)
            {
                var lockedUntil = DateTime.UtcNow.Add(window);
                _db.StringSet(lockKey, lockedUntil.ToString("O"), window);
                _db.KeyDelete(failKey);
            }

            if (emailFailures == 1) _db.KeyExpire(emailFailKey, window);
            if (emailFailures >= _options.MaxFailedLogins * 2)
            {
                var lockedUntil = DateTime.UtcNow.Add(window);
                _db.StringSet(emailLockKey, lockedUntil.ToString("O"), window);
                _db.KeyDelete(emailFailKey);
            }
        }

        public void RegisterFailure(string key) => RegisterFailure(key, "");

        public void Reset(string key, string emailKey)
        {
            var batch = _db.CreateBatch();
            batch.KeyDeleteAsync($"{_prefix}{key}:Failures");
            batch.KeyDeleteAsync($"{_prefix}{key}:LockedUntil");
            batch.KeyDeleteAsync($"{_prefix}{emailKey}:Failures");
            batch.KeyDeleteAsync($"{_prefix}{emailKey}:LockedUntil");
            batch.Execute();
        }

        public void Reset(string key) => Reset(key, "");
    }
}