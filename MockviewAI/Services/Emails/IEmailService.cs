namespace MockviewAI.Services.Emails
{
    /// <summary>
    /// [EMAIL] 
    /// Gửi email HTML qua SMTP (OTP đăng ký, OTP khôi phục, kết quả buổi luyện).
    /// </summary>
    public interface IEmailService
    {
        /// <param name="body">Nội dung HTML; phần chữ do người dùng / AI tạo phải được encode trước.</param>
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}
