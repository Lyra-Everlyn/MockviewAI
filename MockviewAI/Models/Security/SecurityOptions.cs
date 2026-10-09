// [SECURITY-MODULE] SecurityOptions
// Cấu hình bảo mật đọc từ appsettings (mục "Security") hoặc biến môi trường Security__MaxFailedLogins, Security__LockoutMinutes.

namespace MockviewAI.Models.Security
{
    public class SecurityOptions
    {
        // Số lần đăng nhập sai tối đa trước khi bị khóa tạm
        public int MaxFailedLogins { get; set; } = 5;

        // Vừa là thời gian khóa, vừa là khung thời gian đếm số lần sai
        public int LockoutMinutes { get; set; } = 15;
    }
}
