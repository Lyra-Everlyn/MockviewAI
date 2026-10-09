using CloudinaryDotNet;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using MockviewAI.Data;
using MockviewAI.Repositories.Implementations;
using MockviewAI.Repositories.Interfaces;
using MockviewAI.Services.Helper.Implementations;
using MockviewAI.Services.Helper.Interfaces;
using MockviewAI.Services.Implementations;
using MockviewAI.Services.Interfaces;
using System.Security.Principal;

namespace MockviewAI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            // 0. Database connection string
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            // 1. Configure Entity Framework Core with MySQL
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

            // 2. Register Repositories & Services
            // Repository
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddScoped<IUserRepository, UserRepository>();


            // Service
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IOnboardingService, OnboardingService>();


            // Other services
            // a. Google configuration
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/Auth/Login";
                options.LogoutPath = "/api/auth/logout";
            })
            .AddGoogle(options =>
            {
                options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? throw new InvalidOperationException("Google ClientId is missing.");
                options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? throw new InvalidOperationException("Google ClientSecret is missing.");
                options.ClaimActions.MapJsonKey("urn:google:picture", "picture", "url");
            });


            // b. Register Cloudinary
            var cloudinarySettings = builder.Configuration.GetSection("CloudinarySettings");
            string cloudName = cloudinarySettings["CloudName"] ?? throw new InvalidOperationException("Cloudinary CloudName is missing.");
            string apiKey = cloudinarySettings["ApiKey"] ?? throw new InvalidOperationException("Cloudinary ApiKey is missing.");
            string apiSecret = cloudinarySettings["ApiSecret"] ?? throw new InvalidOperationException("Cloudinary ApiSecret is missing.");

            var cloudinaryAccount = new Account(cloudName, apiKey, apiSecret);
            var cloudinary = new Cloudinary(cloudinaryAccount);
            builder.Services.AddSingleton(cloudinary);


            // c. Email service configuration
            builder.Services.AddScoped<IEmailService, EmailService>();


            // 3. Add Memory Cache
            builder.Services.AddMemoryCache();



            // Add services to the container.
            builder.Services.AddControllersWithViews();


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                //app.UseExceptionHandler("/Home/Error");
                app.UseExceptionHandler("/Auth/Login");
                app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication(); // MUST come before UseAuthorization, otherwise the login cookie is never read
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
