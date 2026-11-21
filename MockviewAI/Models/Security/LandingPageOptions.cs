namespace MockviewAI.Models.Security
{
    /// <summary>
    /// Cấu hình landing page (trang giới thiệu tĩnh, chạy ở tên miền khác với ứng dụng).
    /// Bind từ appsettings.json, mục "LandingPage", hoặc biến môi trường
    /// LandingPage__AllowedReturnOrigins__0, LandingPage__AllowedReturnOrigins__1, ...
    /// </summary>
    public class LandingPageOptions
    {
        public const string SectionName = "LandingPage";

        /// <summary>
        /// Các nguồn (scheme + host + port, ví dụ "https://mockviewai.vercel.app") mà trang đăng nhập
        /// được phép đưa người dùng quay về sau khi đăng nhập / đăng ký (tham số ReturnUrl).
        /// Địa chỉ không nằm trong danh sách bị bỏ qua để chống tấn công chuyển hướng mở (open redirect).
        /// </summary>
        public string[] AllowedReturnOrigins { get; set; } = [];

        /// <summary>
        /// Trang chủ landing page. Đăng nhập / đăng ký xong mà không có ReturnUrl hợp lệ
        /// (ví dụ vào thẳng tên miền ứng dụng) thì đưa người dùng về đây thay vì Home/Index.
        /// Phải thuộc AllowedReturnOrigins. Để trống = giữ hành vi cũ (vào Home/Index hoặc Onboarding).
        /// </summary>
        public string? HomeUrl { get; set; }
    }
}
