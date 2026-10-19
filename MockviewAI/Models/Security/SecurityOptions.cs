namespace MockviewAI.Models.Security
{
    /// <summary>
    /// Cấu hình bảo mật ứng dụng. 
    /// Dữ liệu được bind tự động từ appsettings.json (mục "Security") hoặc biến môi trường.
    /// </summary>
    public class SecurityOptions
    {
        /// <summary>
        /// Số lần đăng nhập sai tối đa cho phép trước khi tài khoản/IP bị khóa tạm thời.
        /// </summary>
        public int MaxFailedLogins { get; set; } = 5;

        /// <summary>
        /// Thời gian khóa (tính bằng phút). 
        /// Đồng thời cũng là khung thời gian (window) để đếm số lần đăng nhập sai.
        /// </summary>
        public int LockoutMinutes { get; set; } = 15;
    }
}