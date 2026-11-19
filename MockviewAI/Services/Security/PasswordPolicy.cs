namespace MockviewAI.Services.Security
{
    /// <summary>
    /// [AUTH]
    /// Quy tắc mật khẩu: 8–72 ký tự (BCrypt chỉ đọc 72 byte đầu), có chữ hoa, chữ thường và chữ số.
    /// </summary>
    public static class PasswordPolicy
    {
        public const int MinLength = 8;
        public const int MaxLength = 72;

        /// <summary>Trả về thông báo lỗi, hoặc null nếu mật khẩu hợp lệ.</summary>
        public static string? Validate(string? password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < MinLength)
                return $"Mật khẩu phải có ít nhất {MinLength} ký tự.";

            if (password.Length > MaxLength)
                return $"Mật khẩu tối đa {MaxLength} ký tự.";

            if (!password.Any(char.IsUpper))
                return "Mật khẩu phải có ít nhất một chữ hoa.";

            if (!password.Any(char.IsLower))
                return "Mật khẩu phải có ít nhất một chữ thường.";

            if (!password.Any(char.IsDigit))
                return "Mật khẩu phải có ít nhất một chữ số.";

            return null;
        }
    }
}
