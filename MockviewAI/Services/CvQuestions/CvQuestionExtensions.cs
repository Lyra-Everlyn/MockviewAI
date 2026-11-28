using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using MockviewAI.Services.CvQuestions.Implementations;
using MockviewAI.Services.CvQuestions.Interfaces;

namespace MockviewAI.Services.CvQuestions
{
    /// <summary>
    /// [CV-QUESTIONS] [IT-ROLES]
    /// Đăng ký "Hỏi theo CV" + câu kỹ thuật theo vị trí: người soạn (Gemini), người kiểm duyệt (Groq), luật code, pipeline, nơi lưu,
    /// và AI gợi ý vị trí từ CV (kèm giới hạn lượt gọi).
    /// Dùng: services.AddCvQuestions(config); (cần HttpClient "GeminiClient", "GroqClient" đã đăng ký trước).
    /// </summary>
    public static class CvQuestionExtensions
    {
        /// <summary>Giới hạn lượt gọi /Interview/SuggestRole: 20 lần / 10 phút / tài khoản.</summary>
        public const string RoleSuggestRateLimitPolicy = "RoleSuggestRateLimit";

        public static IServiceCollection AddCvQuestions(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<CvQuestionOptions>(config.GetSection(CvQuestionOptions.SectionName));

            services.AddSingleton<ICvQuestionPromptBuilder, CvQuestionPromptBuilder>();
            services.AddSingleton<ICvQuestionRules, CvQuestionRules>();
            services.AddSingleton<IReviewerCooldown>(_ => new ReviewerCooldown(TimeProvider.System));
            services.AddScoped<ICvQuestionGenerator, GeminiCvQuestionGenerator>();
            services.AddScoped<ICvQuestionReviewer, GroqCvQuestionReviewer>();
            services.AddScoped<ICvQuestionPipeline, CvQuestionPipeline>();
            services.AddScoped<ICvQuestionStore, EfCvQuestionStore>();
            services.AddScoped<IRoleSuggester, RoleSuggester>();

            services.Configure<RateLimiterOptions>(options =>
            {
                options.AddPolicy(RoleSuggestRateLimitPolicy, context =>
                {
                    var userKey = context.User.FindFirst(ClaimTypes.Email)?.Value
                                  ?? context.Connection.RemoteIpAddress?.ToString()
                                  ?? "unknown";
                    return RateLimitPartition.GetFixedWindowLimiter("role:" + userKey, _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(10),
                        QueueLimit = 0
                    });
                });
            });
            return services;
        }
    }
}
