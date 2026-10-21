namespace MockviewAI.Services.Security
{
    public interface ILoginThrottleService
    {
        Task<(bool LockedOut, TimeSpan Remaining)> IsLockedOutAsync(string key, string emailKey);
        Task RegisterFailureAsync(string key, string emailKey);
        Task ResetAsync(string key, string emailKey);
    }
}