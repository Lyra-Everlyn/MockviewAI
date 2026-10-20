using MockviewAI.Models.Entities;

namespace MockviewAI.Services.Security
{
    public interface ILoginThrottleService
    {
        bool IsLockedOut(string key, string emailKey, out TimeSpan remaining);
        void RegisterFailure(string key, string emailKey);
        void Reset(string key, string emailKey);


        bool IsLockedOut(string key, out TimeSpan remaining);
        void RegisterFailure(string key);
        void Reset(string key);
    }
}