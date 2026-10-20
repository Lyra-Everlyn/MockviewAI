using MockviewAI.Models.Entities;

namespace MockviewAI.Services.Auth
{
    public interface IAuthService
    {
        // Register
        Task RegisterAsync(string email, string password, string confirmPassword, string firstName, string lastName);
        Task<User> RegisterGoogleAsync(string email, string firstName, string lastName, string? avatarUrl);
        Task SendRegistrationOtpAsync(string email);
        Task<bool> VerifyRegistrationOtpAsync(string email, string code, string ipAddress);

        // Login
        Task<User?> AuthenticateAsync(string email, string password);
        Task<User> AuthenticateGoogleUserAsync(string email, string firstName, string lastName, string? avatarUrl);

        // Forgot Password
        Task<bool> IsIpBlockedAsync(string ipAddress);
        Task RequestPasswordResetAsync(string email);
        Task<bool> VerifyResetCodeAsync(string email, string code, string ipAddress);
        Task ResetPasswordAsync(string email, string newPassword);
    }
}