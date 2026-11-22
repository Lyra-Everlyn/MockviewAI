namespace MockviewAI.Services.Exceptions
{
    /// <summary>
    /// [AUTH] 
    /// Lỗi nghiệp vụ đăng nhập / đăng ký / khôi phục; message an toàn để hiện cho người dùng.
    /// </summary>
    public class AuthException : Exception
    {
        public AuthException(string message) : base(message) { }

        /// <summary>Sai email hoặc mật khẩu: được tính vào số lần đăng nhập sai.</summary>
        public bool IsCredentialFailure { get; init; }
    }
}
