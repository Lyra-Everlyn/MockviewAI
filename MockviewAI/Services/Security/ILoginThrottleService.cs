namespace MockviewAI.Services.Security
{
    /// <summary>
    /// [SECURITY] 
    /// Đếm lần đăng nhập sai và khoá tạm thời.
    /// Hai khoá đếm song song: <c>key</c> = email + IP, <c>emailKey</c> = chỉ email (dựng bằng LoginThrottleService.BuildKey / BuildEmailKey).
    /// </summary>
    public interface ILoginThrottleService
    {
        /// <returns>Đang bị khoá không, và còn bao lâu nữa.</returns>
        Task<(bool LockedOut, TimeSpan Remaining)> IsLockedOutAsync(string key, string emailKey);

        /// <summary>Ghi nhận một lần sai; đủ ngưỡng thì khoá.</summary>
        Task RegisterFailureAsync(string key, string emailKey);

        /// <summary>Xoá bộ đếm và khoá sau khi đăng nhập thành công.</summary>
        Task ResetAsync(string key, string emailKey);
    }
}
