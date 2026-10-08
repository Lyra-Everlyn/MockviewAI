using MockviewAI.Models.Entities;

namespace MockviewAI.Services.Interfaces
{
    public interface IAuthService
    {
        // Register
        Task RegisterAsync(string email, string password, string firstName, string lastName);
        Task<User> RegisterGoogleAsync(string email, string firstName, string lastName, string? avatarUrl);
        
        // Login
        Task<User?> AuthenticateAsync(string email, string password);
        Task<User> AuthenticateGoogleUserAsync(string email, string firstName, string lastName, string? avatarUrl);


    }
}
