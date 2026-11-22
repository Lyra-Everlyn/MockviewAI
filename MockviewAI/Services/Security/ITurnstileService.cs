namespace MockviewAI.Services.Security
{
    /// <summary>
    /// [SECURITY] 
    /// Xác minh token Cloudflare Turnstile (CAPTCHA) trên form Đăng nhập, Đăng ký, Quên mật khẩu.
    /// </summary>
    public interface ITurnstileService
    {
        /// <param name="token">Giá trị trường "cf-turnstile-response" của form.</param>
        /// <param name="ipAddress">IP người dùng, gửi kèm để Cloudflare đối chiếu.</param>
        /// <returns>true = hợp lệ. Token trống, lỗi mạng hoặc phản hồi lạ đều trả false.</returns>
        Task<bool> VerifyAsync(string token, string? ipAddress);
    }
}
