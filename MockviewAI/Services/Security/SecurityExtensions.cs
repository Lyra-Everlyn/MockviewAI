using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using MockviewAI.Models.Security;

namespace MockviewAI.Services.Security
{
    /// <summary>
    /// [SECURITY] 
    /// Đóng gói cấu hình bảo mật: chống dò mật khẩu, CAPTCHA, proxy (Traefik), khoá mã hoá cookie, HTTP header.
    /// </summary>
    public static class SecurityExtensions
    {
        #region Content Security Policy
        /// <summary>
        /// Đang chạy chế độ Report-Only: trình duyệt chỉ báo vi phạm ở Console, chưa chặn.
        /// Kiểm tra mọi trang không còn cảnh báo thì đổi thành true.
        /// </summary>
        private const bool CspEnforce = false;

        /// <summary>Các nguồn được phép nạp script, style, font, ảnh, khung (CDN, Cloudflare Turnstile).</summary>
        private static readonly string ContentSecurityPolicy = string.Join("; ",
            "default-src 'self'",
            "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://challenges.cloudflare.com",
            "style-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com",
            "font-src 'self' data: https://cdnjs.cloudflare.com",
            "img-src 'self' data: blob: https:",
            "media-src 'self' blob:",
            "connect-src 'self'",
            "frame-src https://challenges.cloudflare.com",
            "object-src 'none'",
            "base-uri 'self'",
            "form-action 'self'",
            "frame-ancestors 'none'");
        #endregion

        #region Đăng ký dịch vụ
        /// <summary>Dùng: builder.Services.AddAppSecurity(builder.Configuration);</summary>
        public static IServiceCollection AddAppSecurity(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<SecurityOptions>(config.GetSection("Security"));
            services.AddSingleton<ILoginThrottleService, LoginThrottleService>();
            services.AddHttpClient<ITurnstileService, TurnstileService>(c => c.Timeout = TimeSpan.FromSeconds(10));

            // Tin header X-Forwarded-* chỉ khi đến từ mạng nội bộ Docker (Traefik), để lấy đúng IP và https của người dùng
            services.Configure<ForwardedHeadersOptions>(o =>
            {
                o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                o.ForwardLimit = 1;
                o.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("10.0.0.0/8"));
                o.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("172.16.0.0/12"));
                o.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("192.168.0.0/16"));
            });

            // Khoá mã hoá cookie lưu ra đĩa để không bị đăng xuất hàng loạt mỗi lần container khởi động lại
            var keysPath = config["DataProtection:KeysPath"];
            if (string.IsNullOrWhiteSpace(keysPath))
                keysPath = Path.Combine(AppContext.BaseDirectory, "keys");
            services.AddDataProtection()
                .SetApplicationName("MockviewAI")
                .PersistKeysToFileSystem(new DirectoryInfo(keysPath));

            return services;
        }

        /// <summary>Cookie đăng nhập: HttpOnly, SameSite=Lax, sống 8 giờ (trượt). Dùng trong AddCookie: options.HardenCookie();</summary>
        public static void HardenCookie(this CookieAuthenticationOptions options)
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;   // Lax vẫn cho Google redirect về
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
        }
        #endregion

        #region Pipeline
        /// <summary>Đọc header proxy rồi gắn header bảo mật cho mọi phản hồi. Dùng ngay sau ErrorHandlingMiddleware.</summary>
        public static IApplicationBuilder UseAppSecurity(this IApplicationBuilder app)
        {
            app.UseForwardedHeaders();

            app.Use(async (context, next) =>
            {
                var headers = context.Response.Headers;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers["Permissions-Policy"] = "camera=(self), microphone=(self), geolocation=()";
                headers[CspEnforce ? "Content-Security-Policy" : "Content-Security-Policy-Report-Only"] = ContentSecurityPolicy;

                await next();
            });

            return app;
        }
        #endregion
    }
}
