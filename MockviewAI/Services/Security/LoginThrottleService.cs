using System.Net;
using Microsoft.Extensions.Options;
using MockviewAI.Models.Security;

namespace MockviewAI.Services.Security
{
    /// <summary>
    /// Dịch vụ kiểm soát tần suất đăng nhập (Rate Limiting / Throttling).
    /// Chống tấn công Brute-force bằng cách khóa tạm thời nếu đăng nhập sai quá nhiều lần.
    /// Cơ chế theo dõi dựa trên sự kết hợp giữa "Email + IP Address" để tránh việc kẻ xấu cố tình khóa tài khoản người khác.
    /// LƯU Ý: Đang lưu trữ trong Memory Cache (Singleton), dữ liệu sẽ reset khi restart app. Nên cân nhắc dùng Redis khi scale up.
    /// </summary>
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

        /// <summary>
        /// Tạo key định danh duy nhất kết hợp giữa Email và IP.
        /// </summary>
        public static string BuildKey(string email, IPAddress? ip) =>
            $"{(email ?? string.Empty).Trim().ToLowerInvariant()}|{ip}";

        /// <summary>
        /// Kiểm tra xem người dùng hiện tại có đang bị khóa đăng nhập hay không.
        /// </summary>
        /// <param name="key">Định danh được tạo từ BuildKey.</param>
        /// <param name="remaining">Thời gian khóa còn lại (nếu có).</param>
        /// <returns>True nếu đang bị khóa, False nếu được phép đăng nhập.</returns>
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

        /// <summary>
        /// Ghi nhận một lần đăng nhập thất bại và tự động khóa nếu vượt quá giới hạn.
        /// </summary>
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

        /// <summary>
        /// Xóa bỏ trạng thái ghi nhận khi người dùng đăng nhập thành công.
        /// </summary>
        public void Reset(string key)
        {
            lock (_lock)
            {
                _entries.Remove(key);
            }
        }

        /// <summary>
        /// Dọn dẹp bộ nhớ (Garbage Collection nội bộ), xóa bỏ các bản ghi đã hết hạn.
        /// </summary>
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