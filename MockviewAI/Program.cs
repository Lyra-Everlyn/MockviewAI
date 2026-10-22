using CloudinaryDotNet;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MockviewAI.Data;
using MockviewAI.Repositories.Implementations;
using MockviewAI.Repositories.Interfaces;
using MockviewAI.Services.Ai;
using MockviewAI.Services.Ai.Implementations;
using MockviewAI.Services.Ai.Interfaces;
using MockviewAI.Services.Auth;
using MockviewAI.Services.Emails;
using MockviewAI.Services.Feedbacks;
using MockviewAI.Services.FileStorage.Implementations;
using MockviewAI.Services.FileStorage.Interfaces;
using MockviewAI.Services.Interviews;
using MockviewAI.Services.Onboarding;
using MockviewAI.Services.Profiles;
using MockviewAI.Services.Security;
using MockviewAI.Services.SessionHistory;
using StackExchange.Redis;
using System.Security.Principal;
using System.Threading.RateLimiting;


namespace MockviewAI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            // =========================================================================
            // PART 1: CONNECTION CONFIGURATION

            // --- A. Database
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Database connection string is missing.");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySql(
                    connectionString,
                    new MySqlServerVersion(new Version(8, 0, 0)),
                    mysqlOptions => mysqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null
                    )
                )
            );

            // --- B. Redis
            // Thiếu cấu hình -> dùng Redis local; AbortOnConnectFail=false để Redis chưa bật thì app vẫn khởi động được
            var redisConnectionString = builder.Configuration.GetConnectionString("RedisConnection");
            if (string.IsNullOrWhiteSpace(redisConnectionString)) redisConnectionString = "localhost:6379";

            var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
            redisOptions.AbortOnConnectFail = false;

            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.ConfigurationOptions = redisOptions;
                options.InstanceName = "MockviewAI_";
            });

            builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisOptions));



            // =========================================================================
            // PART 2: REGISTERING REPOSITORIES & SERVICES FOR THE MOCKVIEW AI PROJECT

            // --- A. REPOSITORIES ---
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddScoped<IUserRepository, UserRepository>();


            // --- B. SERVICES ---
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IOnboardingService, OnboardingService>();
            builder.Services.AddScoped<IFeedbackService, FeedbackService>();
            builder.Services.AddScoped<IInterviewService, InterviewService>();
            builder.Services.AddScoped<IInterviewScoringService, InterviewScoringService>();
            builder.Services.AddScoped<ISessionHistoryService, SessionHistoryService>();
            builder.Services.AddScoped<IProfileService, ProfileService>();
            builder.Services.AddScoped<IEmailService, EmailService>();


            // Configure the AI ​​Provider based on appsettings ("Mock" or "Gemini")
            builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));
            builder.Services.AddSingleton<IDeliveryAnalyzer, DeliveryAnalyzer>();
            builder.Services.AddSingleton<IAnxietyEstimator, AnxietyEstimator>();
            builder.Services.AddSingleton<IAnswerQualityChecker, AnswerQualityChecker>();

            if (string.Equals(builder.Configuration["Ai:Provider"], "Gemini", StringComparison.OrdinalIgnoreCase))
            {
                builder.Services.AddSingleton<IScoringPromptBuilder, ScoringPromptBuilder>();
                builder.Services.AddSingleton<IAiResponseParser, GeminiResponseParser>();
                builder.Services.AddHttpClient<IAiScoringService, GeminiAiScoringService>(c =>
                {
                    c.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
                    c.Timeout = TimeSpan.FromSeconds(60);
                });
            }
            else
            {
                builder.Services.AddScoped<IAiScoringService, MockAiScoringService>();
            }



            // =========================================================================
            // PART 3: THIRD-PARTY SERVICE

            // --- A. GOOGLE AUTHENTICATION & COOKIES ---
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/Auth/Login";
                options.LogoutPath = "/Auth/Logout";

                // Secure cookie configuration:
                // Enable HttpOnly (to prevent XSS) and SameSite (to prevent CSRF), and set a lifespan of 8 hours
                options.HardenCookie();
            })
            .AddGoogle(options =>
            {
                options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? throw new InvalidOperationException("Google ClientId is missing.");
                options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? throw new InvalidOperationException("Google ClientSecret is missing.");
                options.ClaimActions.MapJsonKey("urn:google:picture", "picture", "url");
            });

            // --- B. CLOUDINARY (Media Storage) ---
            var cloudinarySettings = builder.Configuration.GetSection("CloudinarySettings");
            string cloudName = cloudinarySettings["CloudName"] ?? throw new InvalidOperationException("Cloudinary CloudName is missing.");
            string apiKey = cloudinarySettings["ApiKey"] ?? throw new InvalidOperationException("Cloudinary ApiKey is missing.");
            string apiSecret = cloudinarySettings["ApiSecret"] ?? throw new InvalidOperationException("Cloudinary ApiSecret is missing.");

            var cloudinaryAccount = new Account(cloudName, apiKey, apiSecret);
            var cloudinary = new Cloudinary(cloudinaryAccount);
            builder.Services.AddSingleton(cloudinary);
            builder.Services.AddScoped<IFileStorageService, CloudinaryStorageService>();


            // =========================================================================
            // PART 4: CONFIGURATION VIA VISUAL STUDIO & SECURITY

            builder.Services.AddControllersWithViews();

            // --- A. Register security services:
            // Password-guessing protection (throttling), proxy configuration, and system security policies
            builder.Services.AddAppSecurity(builder.Configuration);

            // --- B. IP-BASED RATE LIMITING CONFIGURATION ---
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // 1. Define a policy to limit OTP sending: 3 times / 15 minutes / 1 IP
                options.AddPolicy("OtpRateLimit", context =>
                {
                    // Get the user's real IP (accounting for ForwardedHeaders if a proxy is used)
                    var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: ipAddress,
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 3,
                            Window = TimeSpan.FromMinutes(15),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                });

                options.AddPolicy("LoginRateLimit", context =>
                {
                    var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip";
                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: ipAddress,
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                });

                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = 429;
                    context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";

                    await context.HttpContext.Response.WriteAsync("Too many requests from this IP. Please try again in a few minutes.", token);
                };
            });

            var app = builder.Build();



            // =========================================================================
            // PART 5: MIDDLEWARE PIPELINE

            // --- 0. Chốt chặn lỗi toàn app (đặt ĐẦU TIÊN để bắt được mọi lỗi phía sau)
            app.UseMiddleware<MockviewAI.Middleware.ErrorHandlingMiddleware>();

            // --- A. Standard security: Forwarded headers & HTTP security headers
            app.UseAppSecurity();


            // Configuring error handling and HTTPS for a production environment
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // --- B. Auto update database (Migration)
            // WARNING: Bạn sẽ bị mất toàn bộ dữ liệu database, chỉ thực hiện trên local
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                // Có Migrations -> Migrate(); chưa có thì tự tạo bảng + thêm cột còn thiếu (xem DatabaseInitializer)
                DatabaseInitializer.Initialize(dbContext, scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer"));

                //if (app.Environment.IsDevelopment())
                //{
                //    // 1. Delete all database data
                //    dbContext.Database.EnsureDeleted();

                //    // 2. Recreate a new Database
                //    dbContext.Database.EnsureCreated();
                //}
                //else
                //{
                //    // Must has Migration on Product
                //    dbContext.Database.Migrate();
                //}
            }

            //app.UseHttpsRedirection();

            app.UseRouting();

            // --- C. Enable rate limitter (MUST BE PLACED AFTER UseRouting AND BEFORE UseAuth) ---
            app.UseRateLimiter();

            // --- D. Enforce security workflow: Identity check before permission check
            app.UseAuthentication();
            app.UseAuthorization();


            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                //pattern: "{controller=Home}/{action=Index}/{id?}")
                pattern: "{controller=Auth}/{action=Login}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
