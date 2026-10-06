using Microsoft.EntityFrameworkCore;
using MockviewAI.Data;
using MockviewAI.Repositories.Implementations;
using MockviewAI.Repositories.Interfaces;
using MockviewAI.Services.Implementations;
using MockviewAI.Services.Interfaces;

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


            // Service
            builder.Services.AddScoped<IAuthService, AuthService>();





            // Add services to the container.
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
