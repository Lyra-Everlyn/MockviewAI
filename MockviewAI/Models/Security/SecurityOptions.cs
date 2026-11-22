namespace MockviewAI.Models.Security
{
    /// <summary>
    /// [SECURITY] 
    /// Cấu hình khoá đăng nhập, bind từ mục "Security" (appsettings hoặc biến môi trường Security__*).
    /// </summary>
    public class SecurityOptions
    {
        /// <summary>Số lần đăng nhập sai tối đa trước khi tài khoản / IP bị khoá tạm thời.</summary>
        public int MaxFailedLogins { get; set; } = 5;

        /// <summary>Thời gian khoá (phút), đồng thời là khung thời gian đếm số lần sai.</summary>
        public int LockoutMinutes { get; set; } = 15;
    }
}
