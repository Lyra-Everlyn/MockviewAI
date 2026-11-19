using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace MockviewAI.Services.CvParser
{
    /// <summary>
    /// [USER CV] Đăng ký dịch vụ CV và giới hạn số lần phân tích.
    /// Dùng: builder.Services.AddCvServices(builder.Configuration);
    /// </summary>
    public static class CvServiceExtensions
    {
        public const string RateLimitPolicy = "CvRateLimit";

        #region Registration
        public static IServiceCollection AddCvServices(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<CvOptions>(config.GetSection(CvOptions.SectionName));

            services.AddSingleton<ICvFileValidator, PdfCvFileValidator>();
            services.AddSingleton<ICvExtractionService, CvExtractionService>();
            services.AddScoped<ICvParsingService, GeminiCvParsingService>();
            services.AddScoped<ICvStore, RedisCvStore>();
            services.AddScoped<ICvService, CvService>();

            // 5 lần / 10 phút / tài khoản
            services.Configure<RateLimiterOptions>(options =>
            {
                options.AddPolicy(RateLimitPolicy, context =>
                {
                    var userKey = context.User.FindFirst(ClaimTypes.Email)?.Value
                                  ?? context.Connection.RemoteIpAddress?.ToString()
                                  ?? "unknown";
                    return RateLimitPartition.GetFixedWindowLimiter("cv:" + userKey, _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(10),
                        QueueLimit = 0
                    });
                });
            });

            return services;
        }
        #endregion
    }
}
