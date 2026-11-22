using MockviewAI.Models.Entities;

namespace MockviewAI.Services.Auth
{
    /// <summary>
    /// [AUTH] 
    /// Đăng ký qua OTP email, đăng nhập (mật khẩu / Google), khôi phục mật khẩu.
    /// Lỗi nghiệp vụ ném AuthException (message hiển thị được cho người dùng).
    /// </summary>
    public interface IAuthService
    {
        #region Đăng ký
        /// <summary>Kiểm tra form, giữ thông tin chờ 10 phút trong Redis và gửi OTP 6 số (hiệu lực 5 phút, chờ 60 giây mới gửi lại).</summary>
        Task SendRegistrationOtpAsync(string email, string password, string confirmPassword, string firstName, string? lastName);

        /// <summary>Đối chiếu OTP; sai 5 lần thì khoá IP tạm thời.</summary>
        Task<bool> VerifyRegistrationOtpAsync(string email, string code, string ipAddress);

        /// <summary>Tạo tài khoản sau khi OTP đã đúng.</summary>
        Task CompleteRegistrationAsync(string email);

        /// <summary>Tạo tài khoản cho người đăng nhập Google lần đầu.</summary>
        Task<User> RegisterGoogleAsync(string email, string firstName, string lastName, string? avatarUrl);
        #endregion

        #region Đăng nhập
        /// <summary>Kiểm tra email + mật khẩu. Sai thì ném AuthException có IsCredentialFailure = true.</summary>
        Task<User> AuthenticateAsync(string email, string password);

        /// <summary>Đăng nhập Google: chưa có tài khoản thì tạo mới, có rồi thì cập nhật ảnh đại diện.</summary>
        Task<User> AuthenticateGoogleUserAsync(string email, string firstName, string lastName, string? avatarUrl);
        #endregion

        #region Quên mật khẩu
        /// <summary>IP đang bị khoá vì nhập sai OTP quá nhiều lần.</summary>
        Task<bool> IsIpBlockedAsync(string ipAddress);

        /// <summary>Gửi OTP khôi phục; email không tồn tại vẫn trả về bình thường để không lộ email đã đăng ký.</summary>
        Task RequestPasswordResetAsync(string email);

        /// <summary>Đúng mã thì trả về token dùng một lần (sống 10 phút) cho bước đặt lại mật khẩu.</summary>
        Task<string> VerifyResetCodeAsync(string email, string code, string ipAddress);

        /// <summary>Đặt mật khẩu mới nếu token còn hiệu lực; token bị xoá sau khi dùng.</summary>
        Task ResetPasswordAsync(string email, string resetToken, string newPassword);
        #endregion
    }
}
