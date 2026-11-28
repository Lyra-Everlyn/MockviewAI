using System.Security.Claims;
using System.Threading.RateLimiting;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

using MockviewAI.Data;
using MockviewAI.Middleware;
using MockviewAI.Repositories.Implementations;
using MockviewAI.Repositories.Interfaces;
using MockviewAI.Services.Admin;
using MockviewAI.Services.Ai;
using MockviewAI.Services.Ai.Implementations;
using MockviewAI.Services.Ai.Interfaces;
using MockviewAI.Services.Auth;
using MockviewAI.Services.CvParser;
using MockviewAI.Services.Emails;
using MockviewAI.Services.Feedbacks;
using MockviewAI.Services.FileStorage.Implementations;
using MockviewAI.Services.FileStorage.Interfaces;
using MockviewAI.Services.InterviewQuestions.Implementations;
using MockviewAI.Services.InterviewQuestions.Interfaces;
using MockviewAI.Services.Interviews;
using MockviewAI.Services.Interviews.Conversation;
using MockviewAI.Services.Interviews.Results;
using MockviewAI.Services.KeyManagement.ProviderExtensions;
using MockviewAI.Services.Onboarding;
using MockviewAI.Services.Profiles;
using MockviewAI.Services.Security;
using MockviewAI.Services.SessionHistory;
using MockviewAI.Services.Speech;
using MockviewAI.Services.Speech.Implementations;
using MockviewAI.Services.Speech.Interfaces;

namespace MockviewAI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            DotNetEnv.Env.TraversePath().Load();

            var builder = WebApplication.CreateBuilder(args);

            AddDataStores(builder.Services, builder.Configuration);
            AddCoreServices(builder.Services);
            string aiProvider = AddAiServices(builder.Services, builder.Configuration, builder.Environment);
            AddAuthentication(builder.Services, builder.Configuration);
            AddMediaStorage(builder.Services, builder.Configuration);

            builder.Services.AddControllersWithViews();
            builder.Services.AddAppSecurity(builder.Configuration);
            AddRateLimits(builder.Services);

            var app = builder.Build();
            app.Logger.LogInformation("AI scoring provider: {Provider} | Environment: {Env}", aiProvider, app.Environment.EnvironmentName);

            MigrateDatabase(app);
            ConfigurePipeline(app);

            app.Run();
        }

        #region Kết nối dữ liệu
        /// <summary>MySQL (EF Core + Pomelo, tự thử lại khi mất kết nối) và Redis (cache + IConnectionMultiplexer).</summary>
        private static void AddDataStores(IServiceCollection services, IConfiguration config)
        {
            var connectionString = config.GetConnectionString("DefaultConnection")
                                   ?? throw new InvalidOperationException("Database connection string is missing.");

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySql(
                    connectionString,
                    new MySqlServerVersion(new Version(8, 0, 0)),
                    mysqlOptions => mysqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null)));

            var redisConnectionString = config.GetConnectionString("RedisConnection")
                                        ?? throw new InvalidOperationException("Redis connection string is missing.");
            var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
            redisOptions.AbortOnConnectFail = false;

            services.AddStackExchangeRedisCache(options =>
            {
                options.ConfigurationOptions = redisOptions;
                options.InstanceName = "MockviewAI_";
            });
            services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisOptions));
        }
        #endregion

        #region Repository và dịch vụ nghiệp vụ
        private static void AddCoreServices(IServiceCollection services)
        {
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUserRepository, UserRepository>();

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IOnboardingService, OnboardingService>();
            services.AddScoped<IProfileService, ProfileService>();
            services.AddSingleton<IAvatarFileValidator, AvatarFileValidator>();
            services.AddScoped<IInterviewService, InterviewService>();
            services.AddScoped<IScoreRequestFactory, ScoreRequestFactory>();
            services.AddScoped<ISessionResultBuilder, SessionResultBuilder>();
            services.AddScoped<IResultEmailSender, ResultEmailSender>();
            services.AddScoped<IInterviewScoringService, InterviewScoringService>();
            services.AddScoped<ISessionHistoryService, SessionHistoryService>();
            services.AddScoped<IFeedbackService, FeedbackService>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<IEmailService, EmailService>();
        }
        #endregion

        #region AI: chấm điểm, bộ đề, nhà tuyển dụng, giọng nói, CV
        /// <returns>Tên bộ chấm thực tế (Gemini / Mock), để ghi log khi khởi động.</returns>
        private static string AddAiServices(IServiceCollection services, IConfiguration config, IWebHostEnvironment env)
        {
            // Xoay vòng nhiều API key: handler gắn key cho từng request của HttpClient
            services.AddSingleton<GeminiKeyProvider>();
            services.AddSingleton<GroqKeyProvider>();
            services.AddTransient<GeminiKeyHandler>();
            services.AddTransient<GroqKeyHandler>();

            services.AddHttpClient("GeminiClient", client =>
            {
                client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/");
                client.Timeout = TimeSpan.FromSeconds(180);
            }).AddHttpMessageHandler<GeminiKeyHandler>();

            services.AddHttpClient("GroqClient", client =>
            {
                client.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
            }).AddHttpMessageHandler<GroqKeyHandler>();

            // [AI-SCORING] Chấm cách nói bằng code, chấm nội dung bằng Gemini (Mock khi Development thiếu key)
            services.Configure<GeminiOptions>(config.GetSection("Gemini"));
            services.AddSingleton<IDeliveryAnalyzer, DeliveryAnalyzer>();
            services.AddSingleton<IAnxietyEstimator, AnxietyEstimator>();
            services.AddSingleton<IAnswerQualityChecker, AnswerQualityChecker>();
            string aiProvider = services.AddAiScoring(config, env);

            // [QUESTION-BANK] Đọc Data/InterviewQuestions/question-bank.v1.json một lần khi chạy
            services.AddSingleton<IQuestionBankStore>(sp => new JsonQuestionBankStore(
                Path.Combine(sp.GetRequiredService<IWebHostEnvironment>().ContentRootPath, "Data", "InterviewQuestions")));
            services.AddSingleton<IQuestionSelector>(sp => new RandomQuestionSelector(sp.GetRequiredService<IQuestionBankStore>()));
            services.AddSingleton<IAnswerVerdictValidator, AnswerVerdictValidator>();
            services.AddSingleton<IOfflineAnswerScorer, BankKeywordScorer>();
            services.AddSingleton<IFollowUpPlanner, FollowUpPlanner>();

            // [INTERVIEWER] Lời phản hồi + câu hỏi nối tiếp theo CV
            services.AddInterviewerConversation(config);

            // [SPEECH] Groq Whisper chép lời -> Gemini chuẩn hoá
            services.Configure<SpeechOptions>(config.GetSection(SpeechOptions.SectionName));
            services.AddSingleton<ISpeechPromptBuilder, SpeechPromptBuilder>();
            services.AddSingleton<IHallucinationFilter, WhisperHallucinationFilter>();
            services.AddScoped<ISpeechTranscriber, GroqWhisperTranscriber>();
            services.AddScoped<ITranscriptNormalizer, GeminiTranscriptNormalizer>();
            services.AddScoped<ISpeechToTextService, SpeechToTextService>();

            // [USER CV] PDF -> PdfPig -> Gemini -> Redis
            services.AddCvServices(config);

            return aiProvider;
        }
        #endregion

        #region Đăng nhập (Cookie + Google) và quyền trang quản trị
        private static void AddAuthentication(IServiceCollection services, IConfiguration config)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/Auth/Login";
                options.LogoutPath = "/Auth/Logout";
                options.AccessDeniedPath = "/Home";   // [ADMIN] Không đủ quyền vào /Admin -> về trang chủ
                options.HardenCookie();   // HttpOnly, SameSite=Lax, sống 8 giờ (trượt)
            })
            .AddGoogle(options =>
            {
                options.ClientId = config["Authentication:Google:ClientId"] ?? throw new InvalidOperationException("Google ClientId is missing.");
                options.ClientSecret = config["Authentication:Google:ClientSecret"] ?? throw new InvalidOperationException("Google ClientSecret is missing.");
                options.ClaimActions.MapJsonKey("urn:google:picture", "picture", "url");
            });

            // [ADMIN] Trang quản trị chỉ xem: email khai báo ở Admin__Emails__N (không lưu quyền trong database)
            services.Configure<AdminOptions>(config.GetSection(AdminOptions.SectionName));
            services.AddSingleton<IAdminAccess, AdminAccess>();
            services.AddSingleton<IAuthorizationHandler, AdminAuthorizationHandler>();
            services.AddAuthorization(options =>
                options.AddPolicy(AdminAccess.PolicyName, policy => policy.RequireAuthenticatedUser().AddRequirements(new AdminRequirement())));
        }
        #endregion

        #region Lưu trữ ảnh (Cloudinary)
        private static void AddMediaStorage(IServiceCollection services, IConfiguration config)
        {
            var settings = config.GetSection("CloudinarySettings");
            string cloudName = settings["CloudName"] ?? throw new InvalidOperationException("Cloudinary CloudName is missing.");
            string apiKey = settings["ApiKey"] ?? throw new InvalidOperationException("Cloudinary ApiKey is missing.");
            string apiSecret = settings["ApiSecret"] ?? throw new InvalidOperationException("Cloudinary ApiSecret is missing.");

            services.AddSingleton(new Cloudinary(new Account(cloudName, apiKey, apiSecret)));
            services.AddScoped<IFileStorageService, CloudinaryStorageService>();
        }
        #endregion

        #region Giới hạn tần suất
        /// <summary>
        /// Giới hạn phân tích CV đăng ký riêng trong AddCvServices.
        /// </summary>
        private static void AddRateLimits(IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy("OtpRateLimit", context =>
                    FixedWindow(ClientIp(context), permitLimit: 6, window: TimeSpan.FromMinutes(1)));

                options.AddPolicy("LoginRateLimit", context =>
                    FixedWindow(ClientIp(context), permitLimit: 6, window: TimeSpan.FromMinutes(1)));

                options.AddPolicy("SpeechRateLimit", context =>
                {
                    var userKey = context.User.FindFirst(ClaimTypes.Email)?.Value
                                  ?? context.Connection.RemoteIpAddress?.ToString()
                                  ?? "unknown";
                    return FixedWindow("speech:" + userKey, permitLimit: 20, window: TimeSpan.FromMinutes(1));
                });

                // JS (AudioRecorder) chỉ dựa vào mã 429; form thường thì hiện dòng chữ này
                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
                    await context.HttpContext.Response.WriteAsync("Bạn thao tác quá nhiều lần. Vui lòng thử lại sau ít phút.", token);
                };
            });
        }

        private static string ClientIp(HttpContext context) =>
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip";

        private static RateLimitPartition<string> FixedWindow(string key, int permitLimit, TimeSpan window) =>
            RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = permitLimit,
                Window = window,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
        #endregion

        #region Database và pipeline
        /// <summary>Tự chạy migration còn thiếu mỗi lần khởi động.</summary>
        private static void MigrateDatabase(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();
        }

        /// <summary>
        /// Thứ tự quan trọng: bắt lỗi -> proxy + header bảo mật -> HSTS -> routing -> xác thực -> phân quyền -> giới hạn tần suất.
        /// HTTPS do Traefik (Dokploy) đảm nhận, app chỉ nghe HTTP nội bộ cổng 8080 nên không dùng UseHttpsRedirection.
        /// </summary>
        private static void ConfigurePipeline(WebApplication app)
        {
            // Đứng đầu và là nơi DUY NHẤT xử lý lỗi: request từ fetch nhận JSON { message }, trang thường nhận HTML
            app.UseMiddleware<ErrorHandlingMiddleware>();

            app.UseAppSecurity();
            if (!app.Environment.IsDevelopment())
                app.UseHsts();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseRateLimiter();

            app.MapStaticAssets();
            app.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Auth}/{action=Login}/{id?}")
                .WithStaticAssets();
        }
        #endregion
    }
}
