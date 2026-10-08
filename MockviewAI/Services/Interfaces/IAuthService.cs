using MockviewAI.Models.Entities;

namespace MockviewAI.Services.Interfaces
{
    public interface IAuthService
    {
<<<<<<< Updated upstream
        Task<User?> AuthenticateAsync(string email, string password);
        Task<User> AuthenticateGoogleUserAsync(string email, string fullName);
=======
        // Register
        Task RegisterAsync(string email, string password, string firstName, string lastName);
        Task<User> RegisterGoogleAsync(string email, string firstName, string lastName, string? avatarUrl);
        
        // Login
        Task<User?> AuthenticateAsync(string email, string password);
        Task<User> AuthenticateGoogleUserAsync(string email, string firstName, string lastName, string? avatarUrl);

        // Recovery
        //Task<string> GeneratePasswordResetTokenAsync(string email);
        //Task<bool> VerifyResetTokenAsync(string email, string inputToken);
        //Task ResetPasswordAsync(string email, string inputToken, string newPassword, string confirmPassword);
>>>>>>> Stashed changes
    }
}
