namespace MockviewAI.Services.Security
{
    /// <summary>
    /// Kiểm tra tham số ReturnUrl trỏ về landing page.
    /// Chỉ chấp nhận URL tuyệt đối http/https có nguồn (scheme + host + port) nằm trong danh sách cho phép,
    /// để link dạng /Auth/Login?ReturnUrl=https://trang-gia-mao không thể đưa người dùng sang trang lạ.
    /// </summary>
    public static class ReturnUrlPolicy
    {
        private const int MaxLength = 2048;

        /// <returns>URL đã chuẩn hoá nếu hợp lệ; null nếu rỗng, sai định dạng hoặc không được phép.</returns>
        public static string? GetSafeLandingUrl(string? returnUrl, IEnumerable<string>? allowedOrigins)
        {
            if (string.IsNullOrWhiteSpace(returnUrl) || returnUrl.Length > MaxLength || allowedOrigins is null)
                return null;

            if (!Uri.TryCreate(returnUrl.Trim(), UriKind.Absolute, out var uri))
                return null;

            if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
                return null;

            // Chặn dạng https://ten@trang-khac.com (phần "ten@" dễ đánh lừa người đọc link)
            if (!string.IsNullOrEmpty(uri.UserInfo))
                return null;

            var origin = uri.GetLeftPart(UriPartial.Authority);

            foreach (var allowed in allowedOrigins)
            {
                if (string.IsNullOrWhiteSpace(allowed) || !Uri.TryCreate(allowed.Trim(), UriKind.Absolute, out var allowedUri))
                    continue;

                if (string.Equals(allowedUri.GetLeftPart(UriPartial.Authority), origin, StringComparison.OrdinalIgnoreCase))
                    return uri.AbsoluteUri;
            }

            return null;
        }
    }
}
