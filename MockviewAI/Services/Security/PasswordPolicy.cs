// [SECURITY-MODULE] PasswordPolicy
// Quy tắc mật khẩu mạnh: 8-72 ký tự, có chữ hoa, chữ thường và chữ số.
// (72 vì BCrypt chỉ dùng 72 byte đầu của mật khẩu.)

namespace MockviewAI.Services.Security
{
    public static class PasswordPolicy
    {
        public const int MinLength = 8;
        public const int MaxLength = 72;

        // Trả về null nếu mật khẩu hợp lệ, ngược lại trả về thông báo lỗi để hiển thị cho người dùng
        public static string? Validate(string? password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < MinLength)
                return $"Password must be at least {MinLength} characters long.";
            if (password.Length > MaxLength)
                return $"Password must be at most {MaxLength} characters long.";
            if (!password.Any(char.IsUpper))
                return "Password must contain at least one uppercase letter.";
            if (!password.Any(char.IsLower))
                return "Password must contain at least one lowercase letter.";
            if (!password.Any(char.IsDigit))
                return "Password must contain at least one digit.";
            return null;
        }
    }
}
