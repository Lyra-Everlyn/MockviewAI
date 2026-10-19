using MockviewAI.Models.Entities;

namespace MockviewAI.Services.Security
{
    public interface ILoginThrottleService
    {
        // Check whether the current user is locked out from logging in
        bool IsLockedOut(string key, out TimeSpan remaining);

        // Records a failed login attempt
        void RegisterFailure(string key);

        // Clear the recorded status upon successful user login
        void Reset(string key);
    }
}