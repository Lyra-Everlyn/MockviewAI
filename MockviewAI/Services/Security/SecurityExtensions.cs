using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using MockviewAI.Models.Security;

namespace MockviewAI.Services.Security
{
    /// <summary>
    /// Lớp mở rộng (Extension Methods) giúp đóng gói các cấu hình bảo mật.
    /// Giúp file Program.cs giữ được sự gọn gàng và dễ bảo trì.
    /// </summary>
    public static class SecurityExtensions
    {
        /// <summary>
        /// Đăng ký các dịch vụ bảo mật vào DI Container.
        /// Cách gọi: builder.Services.AddAppSecurity(builder.Configuration);
        /// </summary>
        public static IServiceCollection AddAppSecurity(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<SecurityOptions>(config.GetSection("Security"));
            services.AddSingleton<ILoginThrottleService, LoginThrottleService>();
            services.AddHttpClient<ITurnstileService, TurnstileService>();

            // Cấu hình nhận diện IP thật và giao thức (HTTPS) khi chạy phía sau Proxy (Nginx, Traefik, Docker...)
            services.Configure<ForwardedHeadersOptions>(o =>
            {
                o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

                //o.KnownIPNetworks.Clear();
                //o.KnownProxies.Clear();

                // Chỉ đọc 1 lớp Proxy gần nhất (chống client tự chèn chuỗi X-Forwarded-For giả ở phía trước)
                o.ForwardLimit = 1;

                // Tin tưởng toàn bộ các dải IP nội bộ chuẩn RFC 1918 (Docker, LAN, Cloud VPC) + Loopback mặc định (127.0.0.1)
                o.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("10.0.0.0/8"));
                o.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("172.16.0.0/12"));
                o.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("192.168.0.0/16"));
            });

            return services;
        }

        /// <summary>
        /// Thiết lập các thuộc tính bảo mật nghiêm ngặt cho Authentication Cookie.
        /// Cách gọi: options.HardenCookie(); (Bên trong AddCookie)
        /// </summary>
        public static void HardenCookie(this CookieAuthenticationOptions options)
        {
            // Chống XSS: Ngăn JavaScript truy cập cookie
            options.Cookie.HttpOnly = true;

            // Chống CSRF: Hạn chế gửi cookie qua các request chéo trang (Lax vẫn cho phép Google Redirect)
            options.Cookie.SameSite = SameSiteMode.Lax;

            // Yêu cầu HTTPS trên môi trường Production
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
        }

        /// <summary>
        /// Kích hoạt Middleware bảo mật trong HTTP Pipeline.
        /// Cách gọi: app.UseAppSecurity(); (Bên dưới app.Build())
        /// </summary>
        public static IApplicationBuilder UseAppSecurity(this IApplicationBuilder app)
        {
            // Bắt buộc phải chạy đầu tiên để các Middleware sau nhận đúng IP/Giao thức
            app.UseForwardedHeaders();

            // Bổ sung các HTTP Security Headers cơ bản phòng thủ tấn công phổ biến
            app.Use(async (context, next) =>
            {
                var headers = context.Response.Headers;

                // Ngăn trình duyệt tự ý đoán định dạng file (chống MIME-sniffing)
                headers["X-Content-Type-Options"] = "nosniff";

                // Ngăn trang web bị nhúng vào iframe của domain khác (chống Clickjacking)
                headers["X-Frame-Options"] = "DENY";

                // Bảo mật thông tin Referrer khi điều hướng sang trang khác
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

                // Giới hạn quyền truy cập phần cứng (chỉ cho phép camera/mic cho phòng phỏng vấn, tắt geolocation)
                headers["Permissions-Policy"] = "camera=(self), microphone=(self), geolocation=()";

                await next();
            });

            return app;
        }
    }
}