using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace MockviewAI.Services.Emails
{
    /// <summary>
    /// [EMAIL] 
    /// Gửi email bằng MailKit, cấu hình lấy từ mục SmtpSettings (biến môi trường SmtpSettings__*).
    /// Mỗi lần gửi mở một kết nối STARTTLS mới rồi đóng lại.
    /// </summary>
    public class EmailService : IEmailService
    {
        #region Fields
        private readonly IConfiguration _config;
        #endregion

        #region Constructor
        public EmailService(IConfiguration config)
        {
            _config = config;
        }
        #endregion

        #region IEmailService
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_config["SmtpSettings:SenderName"], _config["SmtpSettings:SenderEmail"]!));
            email.To.Add(new MailboxAddress("", toEmail));
            email.Subject = subject;
            email.Body = new BodyBuilder { HtmlBody = body }.ToMessageBody();

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(_config["SmtpSettings:Server"]!, int.Parse(_config["SmtpSettings:Port"]!), SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_config["SmtpSettings:Username"]!, _config["SmtpSettings:Password"]!);

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
        #endregion
    }
}
