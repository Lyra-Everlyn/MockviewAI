using MockviewAI.Models.Entities;

namespace MockviewAI.Services.Auth
{
    /// <summary>
    /// [AUTH] 
    /// Đăng ký, đăng nhập, khôi phục mật khẩu. Lỗi nghiệp vụ ném AuthException.
    /// </summary>
    public interface IAuthService
    {
        #region Register
        Task SendRegistrationOtpAsync(string email, string password, string confirmPassword, string firstName, string? lastName);
        Task<bool> VerifyRegistrationOtpAsync(string email, string code, string ipAddress);
        Task CompleteRegistrationAsync(string email);
        Task<User> RegisterGoogleAsync(string email, string firstName, string lastName, string? avatarUrl);
        #endregion

        #region Login
        Task<User> AuthenticateAsync(string email, string password);
        Task<User> AuthenticateGoogleUserAsync(string email, string firstName, string lastName, string? avatarUrl);
        #endregion

        #region Forgot password
        Task<bool> IsIpBlockedAsync(string ipAddress);
        Task RequestPasswordResetAsync(string email);

        /// <summary>Đúng mã thì trả về token dùng một lần cho bước đặt lại mật khẩu.</summary>
        Task<string> VerifyResetCodeAsync(string email, string code, string ipAddress);

        Task ResetPasswordAsync(string email, string resetToken, string newPassword);
        #endregion
    }
}
