namespace MockviewAI.Services.Interviews.Conversation
{
    /// <summary>
    /// [INTERVIEWER] 
    /// Đăng ký nhà tuyển dụng trò chuyện. Dùng: builder.Services.AddInterviewerConversation(builder.Configuration);
    /// Cần đăng ký sau GeminiKeyProvider và HttpClient "GeminiClient".
    /// </summary>
    public static class InterviewerConversationExtensions
    {
        public static IServiceCollection AddInterviewerConversation(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<InterviewerOptions>(config.GetSection(InterviewerOptions.SectionName));
            services.AddScoped<ICandidateCvStore, RedisCandidateCvStore>();
            services.AddScoped<IInterviewerTurnService, GeminiInterviewerTurnService>();
            return services;
        }
    }
}
