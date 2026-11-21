using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MockviewAI.Services.Security
{
    /// <summary>
    /// Báo cho landing page (tên miền khác, không đọc được cookie đăng nhập của ứng dụng) biết trạng thái tài khoản,
    /// để landing đổi nút "Đăng nhập" thành ảnh đại diện.
    ///
    /// Cách làm: khi chuyển hướng về landing, gắn vào phần # của URL:
    ///   #mv-auth=&lt;base64url JSON { n: tên hiển thị, a: ảnh đại diện https | null, k: mã tài khoản, e: hết hạn (Unix giây) }&gt;
    ///   #mv-auth=out   (vừa đăng xuất)
    /// Phần # không được trình duyệt gửi lên máy chủ. Dữ liệu chỉ dùng để HIỂN THỊ (không phải token đăng nhập):
    /// ai sửa tay chuỗi này cũng chỉ đổi được giao diện trên máy của chính họ, không vào được tài khoản.
    /// Neo (#muc) có sẵn trong URL được giữ lại qua tham số "to".
    /// </summary>
    public static class LandingAuthHandoff
    {
        public const string Key = "mv-auth";
        public const string SignedOut = "out";

        /// <summary>Thời gian landing giữ ảnh đại diện; trùng với thời hạn cookie đăng nhập (xem HardenCookie: 8 giờ).</summary>
        private static readonly TimeSpan DisplayLifetime = TimeSpan.FromHours(8);

        public static string SignedIn(string landingUrl, string? displayName, string? avatarUrl, string? email)
        {
            var payload = JsonSerializer.Serialize(new
            {
                n = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
                a = !string.IsNullOrEmpty(avatarUrl) && avatarUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? avatarUrl : null,
                k = AccountKey(email),
                e = DateTimeOffset.UtcNow.Add(DisplayLifetime).ToUnixTimeSeconds()
            });
            return Attach(landingUrl, ToBase64Url(payload));
        }

        /// <summary>
        /// Mã tài khoản để landing tách tiến độ checklist của từng người trên cùng trình duyệt.
        /// Là 16 ký tự đầu của SHA-256(email viết thường) — không đọc ngược ra email, không dùng để đăng nhập.
        /// </summary>
        private static string? AccountKey(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant()));
            return Convert.ToHexString(hash, 0, 8).ToLowerInvariant();
        }

        public static string Out(string landingUrl) => Attach(landingUrl, SignedOut);

        private static string Attach(string landingUrl, string state)
        {
            var hash = landingUrl.IndexOf('#');
            var baseUrl = hash >= 0 ? landingUrl[..hash] : landingUrl;
            var anchor = hash >= 0 ? landingUrl[(hash + 1)..] : string.Empty;

            var fragment = $"{Key}={state}";
            if (anchor.Length > 0) fragment += "&to=" + Uri.EscapeDataString(Uri.UnescapeDataString(anchor));
            return baseUrl + "#" + fragment;
        }

        private static string ToBase64Url(string text) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(text)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
