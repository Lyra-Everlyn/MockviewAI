namespace MockviewAI.Services.Security
{
    /// <summary>
    /// [SECURITY] 
    /// Xác minh token Cloudflare Turnstile (CAPTCHA). Lỗi mạng / phản hồi lạ = không hợp lệ.
    /// </summary>
    public interface ITurnstileService
    {
        Task<bool> VerifyAsync(string token, string? ipAddress);
    }
}
