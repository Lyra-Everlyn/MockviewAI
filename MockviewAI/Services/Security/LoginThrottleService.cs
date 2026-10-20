using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using MockviewAI.Models.Security;
using System.Net;

namespace MockviewAI.Services.Security
{
    public class LoginThrottleService : ILoginThrottleService
    {
        private readonly IDistributedCache _cache;
        private readonly SecurityOptions _options;

        public LoginThrottleService(IDistributedCache cache, IOptions<SecurityOptions> options)
        {
            _cache = cache;
            _options = options.Value;
        }


        public static string BuildKey(string email, IPAddress? ip) =>
            $"Throttle|{(email ?? string.Empty).Trim().ToLowerInvariant()}|{ip}";

        public bool IsLockedOut(string key, out TimeSpan remaining)
        {
            remaining = TimeSpan.Zero;
            var lockedUntilStr = _cache.GetString($"{key}:LockedUntil");
            
            if (!string.IsNullOrEmpty(lockedUntilStr) && DateTime.TryParse(lockedUntilStr, out var lockedUntil))
            {
                var now = DateTime.UtcNow;
                if (lockedUntil > now)
                {
                    remaining = lockedUntil - now;
                    return true;
                }
            }
            return false;
        }

        public void RegisterFailure(string key)
        {
            var failuresStr = _cache.GetString($"{key}:Failures");
            int failures = string.IsNullOrEmpty(failuresStr) ? 0 : int.Parse(failuresStr);
            failures++;

            var window = TimeSpan.FromMinutes(_options.LockoutMinutes);

            if (failures >= _options.MaxFailedLogins)
            {
                var lockedUntil = DateTime.UtcNow.Add(window);
                var lockOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = window };
                
                _cache.SetString($"{key}:LockedUntil", lockedUntil.ToString("O"), lockOptions);
                _cache.Remove($"{key}:Failures");
            }
            else
            {
                var countOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = window };
                _cache.SetString($"{key}:Failures", failures.ToString(), countOptions);
            }
        }

        public void Reset(string key)
        { 
            _cache.Remove($"{key}:Failures");
            _cache.Remove($"{key}:LockedUntil");
        }
    }
}