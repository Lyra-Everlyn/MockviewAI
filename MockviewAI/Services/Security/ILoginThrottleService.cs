namespace MockviewAI.Services.Security
{
    /// <summary>
    /// [SECURITY] 
    /// Đếm lần đăng nhập sai và khoá tạm thời theo email + IP.
    /// </summary>
    public interface ILoginThrottleService
    {
        Task<(bool LockedOut, TimeSpan Remaining)> IsLockedOutAsync(string key, string emailKey);
        Task RegisterFailureAsync(string key, string emailKey);
        Task ResetAsync(string key, string emailKey);
    }
}
