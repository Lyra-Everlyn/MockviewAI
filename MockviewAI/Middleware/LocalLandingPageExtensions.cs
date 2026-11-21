using Microsoft.Extensions.FileProviders;

namespace MockviewAI.Middleware
{
    /// <summary>
    /// CHỈ DÙNG KHI LẬP TRÌNH (Development): cho ứng dụng phục vụ luôn landing page (HTML tĩnh, repo nhánh
    /// website-landing-page) tại http://localhost:9090/landing/ — bấm ▶ trong Visual Studio là chạy được cả hai,
    /// không cần bật Live Server bên VS Code.
    ///
    /// Bật bằng cách khai báo thư mục landing trong .env của máy mình (không commit), ví dụ:
    ///   LandingPage__LocalFolder=C:/Users/ADMIN/source/repos/MockviewAI
    /// Không khai báo, thư mục sai, hoặc đang chạy Production → bỏ qua, ứng dụng chạy như cũ.
    /// </summary>
    public static class LocalLandingPageExtensions
    {
        public const string RequestPath = "/landing";

        public static WebApplication UseLocalLandingPage(this WebApplication app)
        {
            if (!app.Environment.IsDevelopment()) return app;

            var folder = app.Configuration["LandingPage:LocalFolder"];
            if (string.IsNullOrWhiteSpace(folder)) return app;

            var fullPath = Path.GetFullPath(folder.Trim());
            if (!File.Exists(Path.Combine(fullPath, "index.html")))
            {
                app.Logger.LogWarning("LandingPage:LocalFolder = {Folder} không có index.html → không phục vụ landing page.", fullPath);
                return app;
            }

            // PhysicalFileProvider mặc định đã ẩn file/thư mục bắt đầu bằng dấu chấm (.git, .env...).
            var files = new PhysicalFileProvider(fullPath);

            // /landing → /landing/ (để đường dẫn tương đối assets/, bai-viet/... trong trang chạy đúng)
            app.Use(async (context, next) =>
            {
                if (string.Equals(context.Request.Path.Value, RequestPath, StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Redirect(RequestPath + "/" + context.Request.QueryString);
                    return;
                }
                await next();
            });

            app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files, RequestPath = RequestPath });
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = files,
                RequestPath = RequestPath,
                // Sửa file landing xong chỉ cần F5, không bị trình duyệt giữ bản cũ
                OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "no-cache"
            });

            app.Logger.LogInformation("Landing page (máy local): {Path}/ ← {Folder}", RequestPath, fullPath);
            return app;
        }
    }
}
