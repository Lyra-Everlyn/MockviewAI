using MockviewAI.Models.Entities;

namespace MockviewAI.Services.Interfaces
{
    public interface IAuthService
    {
        Task<User?> AuthenticateAsync(string email, string password);
        Task<User> AuthenticateGoogleUserAsync(string email, string fullName);
    }
}
