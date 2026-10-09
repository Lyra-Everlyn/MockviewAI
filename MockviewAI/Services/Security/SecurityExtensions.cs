// [SECURITY-MODULE] SecurityExtensions
// Gom cấu hình bảo mật vào một chỗ để Program.cs chỉ cần vài dòng gọi:
//   builder.Services.AddAppSecurity(builder.Configuration);   // đăng ký dịch vụ
//   cookieOptions => cookieOptions.HardenCookie();             // cookie an toàn
//   app.UseAppSecurity();                                      // forwarded headers + security headers

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using MockviewAI.Models.Security;

namespace MockviewAI.Services.Security
{
    public static class SecurityExtensions
    {
        public static IServiceCollection AddAppSecurity(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<SecurityOptions>(config.GetSection("Security"));
            services.AddSingleton<LoginThrottleService>();

            // Coolify/Traefik chấm dứt HTTPS rồi chuyển tiếp cho app: tin X-Forwarded-* để redirect Google dùng https
            services.Configure<ForwardedHeadersOptions>(o =>
            {
                o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                o.KnownNetworks.Clear();
                o.KnownProxies.Clear();
            });
            return services;
        }

        public static void HardenCookie(this CookieAuthenticationOptions options)
        {
            options.Cookie.HttpOnly = true;                                  // JS không đọc được cookie (chống XSS đánh cắp)
            options.Cookie.SameSite = SameSiteMode.Lax;                      // chống CSRF, vẫn cho Google redirect
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;  // production đi qua proxy HTTPS
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
        }

        public static IApplicationBuilder UseAppSecurity(this IApplicationBuilder app)
        {
            app.UseForwardedHeaders();   // phải chạy trước mọi thứ cần biết IP/giao thức thật

            // Header phòng thủ cơ bản. Chưa bật Content-Security-Policy vì giao diện đang dùng script/style nội tuyến và CDN.
            app.Use(async (context, next) =>
            {
                var h = context.Response.Headers;
                h["X-Content-Type-Options"] = "nosniff";                          // không đoán kiểu file
                h["X-Frame-Options"] = "DENY";                                    // chống clickjacking
                h["Referrer-Policy"] = "strict-origin-when-cross-origin";
                h["Permissions-Policy"] = "camera=(self), microphone=(self), geolocation=()"; // phòng phỏng vấn cần mic/camera
                await next();
            });
            return app;
        }
    }
}
