// [SECURITY-MODULE] LoginThrottleService
// Chống dò mật khẩu (brute force): sai quá nhiều lần thì khóa tạm.
// Đếm theo cặp "email + địa chỉ IP" để kẻ xấu không thể khóa tài khoản của người khác từ IP khác.
// Lưu trong bộ nhớ (singleton): khởi động lại app thì đếm lại từ đầu. Đủ cho đồ án; sản phẩm thật nên dùng Redis.

using System.Net;
using Microsoft.Extensions.Options;
using MockviewAI.Models.Security;

namespace MockviewAI.Services.Security
{
    public class LoginThrottleService
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

        // true nếu đang bị khóa; remaining = thời gian còn lại
        public bool IsLockedOut(string key, out TimeSpan remaining)
        {
            lock (_lock)
            {
                remaining = TimeSpan.Zero;
                if (!_entries.TryGetValue(key, out var e) || e.LockedUntil is null) return false;

                var now = _time.GetUtcNow().UtcDateTime;
                if (e.LockedUntil <= now)
                {
                    _entries.Remove(key);   // hết hạn khóa
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
                    e.LockedUntil = now + window;
            }
        }

        // Gọi khi đăng nhập thành công
        public void Reset(string key)
        {
            lock (_lock) { _entries.Remove(key); }
        }

        // Dọn các mục cũ để bộ nhớ không phình mãi (chỉ chạy khi có nhiều mục)
        private void PurgeExpired(DateTime now, TimeSpan window)
        {
            if (_entries.Count < 5000) return;
            var old = _entries.Where(kv => now - kv.Value.WindowStart > window && (kv.Value.LockedUntil ?? now) <= now)
                              .Select(kv => kv.Key).ToList();
            foreach (var k in old) _entries.Remove(k);
        }
    }
}
