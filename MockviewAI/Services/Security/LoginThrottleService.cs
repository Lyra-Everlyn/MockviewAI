using System.Net;
using Microsoft.Extensions.Options;
using MockviewAI.Models.Security;

namespace MockviewAI.Services.Security
{
    public class LoginThrottleService : ILoginThrottleService
    {
        private sealed class Entry
        {
            public int Failures;
            public DateTime WindowStart;
            public DateTime? LockedUntil;
        }

        private readonly Dictionary<string, Entry> _entries = new();
        private readonly object _lock = new();
        private readonly SecurityOptions _options;
        private readonly TimeProvider _time;

        public LoginThrottleService(IOptions<SecurityOptions> options, TimeProvider? time = null)
        {
            _options = options.Value;
            _time = time ?? TimeProvider.System;
        }


        public static string BuildKey(string email, IPAddress? ip) =>
            $"{(email ?? string.Empty).Trim().ToLowerInvariant()}|{ip}";

        public bool IsLockedOut(string key, out TimeSpan remaining)
        {
            lock (_lock)
            {
                remaining = TimeSpan.Zero;
                if (!_entries.TryGetValue(key, out var e) || e.LockedUntil is null) return false;

                var now = _time.GetUtcNow().UtcDateTime;
                if (e.LockedUntil <= now)
                {
                    _entries.Remove(key);
                    return false;
                }

                remaining = e.LockedUntil.Value - now;
                return true;
            }
        }

        public void RegisterFailure(string key)
        {
            lock (_lock)
            {
                var now = _time.GetUtcNow().UtcDateTime;
                var window = TimeSpan.FromMinutes(_options.LockoutMinutes);

                PurgeExpired(now, window);

                if (!_entries.TryGetValue(key, out var e) || now - e.WindowStart > window)
                {
                    e = new Entry { WindowStart = now };
                    _entries[key] = e;
                }

                e.Failures++;
                if (e.Failures >= _options.MaxFailedLogins)
                {
                    e.LockedUntil = now + window;
                }
            }
        }

        public void Reset(string key)
        {
            lock (_lock)
            {
                _entries.Remove(key);
            }
        }

        private void PurgeExpired(DateTime now, TimeSpan window)
        {
            if (_entries.Count < 5000) return;

            var old = _entries.Where(kv => now - kv.Value.WindowStart > window && (kv.Value.LockedUntil ?? now) <= now)
                              .Select(kv => kv.Key).ToList();

            foreach (var k in old)
            {
                _entries.Remove(k);
            }
        }
    }
}