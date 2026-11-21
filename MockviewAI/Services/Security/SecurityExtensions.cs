using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using MockviewAI.Models.Security;

namespace MockviewAI.Services.Security
{
    /// <summary>
    /// [SECURITY] 
    /// Đóng gói cấu hình bảo mật: dịch vụ chống dò mật khẩu, CAPTCHA, proxy, khoá mã hoá, cookie, HTTP header.
    /// </summary>
    public static class SecurityExtensions
    {
        #region Content Security Policy
        /// <summary>
        /// Nguồn được phép nạp tài nguyên. Đang chạy chế độ Report-Only: trình duyệt chỉ báo vi phạm ở Console, chưa chặn.
        /// Kiểm tra mọi trang không còn cảnh báo thì đổi CspEnforce = true.
        /// </summary>
        private const bool CspEnforce = false;

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

        #region Services
        /// <summary>Dùng: builder.Services.AddAppSecurity(builder.Configuration);</summary>
        public static IServiceCollection AddAppSecurity(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<SecurityOptions>(config.GetSection("Security"));
            services.Configure<LandingPageOptions>(config.GetSection(LandingPageOptions.SectionName));
            services.AddSingleton<ILoginThrottleService, LoginThrottleService>();
            services.AddHttpClient<ITurnstileService, TurnstileService>(c => c.Timeout = TimeSpan.FromSeconds(10));
            services.Configure<ForwardedHeadersOptions>(o =>
            {
                o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                o.ForwardLimit = 1;
                o.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("10.0.0.0/8"));
                o.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("172.16.0.0/12"));
                o.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("192.168.0.0/16"));
            });

            var keysPath = config["DataProtection:KeysPath"];
            if (string.IsNullOrWhiteSpace(keysPath))
                keysPath = Path.Combine(AppContext.BaseDirectory, "keys");
            services.AddDataProtection()
                .SetApplicationName("MockviewAI")
                .PersistKeysToFileSystem(new DirectoryInfo(keysPath));

            return services;
        }

        /// <summary>Dùng trong AddCookie: options.HardenCookie();</summary>
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
        /// <summary>Dùng ngay sau app.Build(): app.UseAppSecurity();</summary>
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
