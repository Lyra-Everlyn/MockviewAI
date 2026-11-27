using MockviewAI.Services.CvQuestions.Implementations;
using MockviewAI.Services.CvQuestions.Interfaces;

namespace MockviewAI.Services.CvQuestions
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// Đăng ký "Hỏi theo CV": người soạn (Gemini), người kiểm duyệt (Groq), luật code, pipeline, nơi lưu.
    /// Dùng: services.AddCvQuestions(config); (cần HttpClient "GeminiClient", "GroqClient" đã đăng ký trước).
    /// </summary>
    public static class CvQuestionExtensions
    {
        public static IServiceCollection AddCvQuestions(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<CvQuestionOptions>(config.GetSection(CvQuestionOptions.SectionName));

            services.AddSingleton<ICvQuestionPromptBuilder, CvQuestionPromptBuilder>();
            services.AddSingleton<ICvQuestionRules, CvQuestionRules>();
            services.AddScoped<ICvQuestionGenerator, GeminiCvQuestionGenerator>();
            services.AddScoped<ICvQuestionReviewer, GroqCvQuestionReviewer>();
            services.AddScoped<ICvQuestionPipeline, CvQuestionPipeline>();
            services.AddScoped<ICvQuestionStore, EfCvQuestionStore>();
            return services;
        }
    }
}
