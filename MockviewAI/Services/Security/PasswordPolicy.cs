namespace MockviewAI.Services.Security
{
    /// <summary>
    /// Định nghĩa các quy tắc kiểm tra độ mạnh của mật khẩu.
    /// Yêu cầu: Độ dài 8-72 ký tự, bao gồm chữ hoa, chữ thường và chữ số.
    /// Lưu ý: Giới hạn tối đa 72 ký tự do thuật toán BCrypt chỉ xử lý 72 byte đầu tiên.
    /// </summary>
    public static class PasswordPolicy
    {
        public const int MinLength = 8;
        public const int MaxLength = 72;

        /// <summary>
        /// Kiểm tra tính hợp lệ của mật khẩu.
        /// </summary>
        /// <param name="password">Mật khẩu thô người dùng nhập vào.</param>
        /// <returns>Trả về chuỗi báo lỗi nếu không hợp lệ. Trả về null nếu mật khẩu đạt chuẩn.</returns>
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