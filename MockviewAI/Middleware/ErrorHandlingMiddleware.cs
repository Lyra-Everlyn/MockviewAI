using System.Net;

namespace MockviewAI.Middleware
{
    /// <summary>
    /// [SYSTEM] 
    /// Chốt chặn lỗi toàn ứng dụng, đăng ký đầu tiên trong pipeline (Program.cs).
    /// Bắt mọi lỗi chưa được xử lý, ghi log, rồi trả JSON (request từ fetch) hoặc trang HTML thân thiện.
    /// </summary>
    public class ErrorHandlingMiddleware
    {
        #region Fields
        private const string FriendlyMessage = "Có lỗi xảy ra. Vui lòng thử lại.";

        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        #endregion

        #region Constructor
        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        #endregion

        #region Pipeline
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // Người dùng đóng tab / bấm huỷ giữa chừng: không phải lỗi
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không mong đợi tại {Method} {Path}", context.Request.Method, context.Request.Path);

                // Đã gửi một phần phản hồi thì không ghi đè được, chỉ còn cách cắt kết nối
                if (context.Response.HasStarted)
                {
                    context.Abort();
                    return;
                }

                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                if (WantsJson(context.Request))
                {
                    context.Response.ContentType = "application/json; charset=utf-8";
                    await context.Response.WriteAsync("{\"message\":\"" + FriendlyMessage + "\"}");
                }
                else
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(BuildHtml(context.TraceIdentifier));
                }
            }
        }
        #endregion

        #region Helpers
        /// <summary>Request gọi bằng fetch cần JSON để hiện thông báo trong trang, không cần cả trang HTML.</summary>
        private static bool WantsJson(HttpRequest request)
        {
            string contentType = request.ContentType ?? string.Empty;
            string accept = request.Headers.Accept.ToString();
            return contentType.Contains("json", StringComparison.OrdinalIgnoreCase)
                   || accept.Contains("application/json", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Trang lỗi tự chứa (CSS nội tuyến, màu cố định) vì lúc này có thể không tải được layout hay file theme.
        /// </summary>
        /// <param name="requestId">Mã tra cứu để đối chiếu với log.</param>
        private static string BuildHtml(string requestId)
        {
            return "<!DOCTYPE html><html lang=\"vi\"><head><meta charset=\"utf-8\">"
                 + "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">"
                 + "<title>Có lỗi xảy ra - MockviewAI</title>"
                 + "<style>body{margin:0;min-height:100vh;display:flex;align-items:center;justify-content:center;background:#f5f7ff;font-family:'Segoe UI',Arial,sans-serif;color:#1f2a44}"
                 + ".box{max-width:420px;margin:16px;padding:32px;background:#fff;border-radius:16px;box-shadow:0 8px 30px rgba(41,73,255,.12);text-align:center}"
                 + "h1{font-size:22px;margin:0 0 8px}p{color:#55607a;line-height:1.5;margin:8px 0}"
                 + "a{display:inline-block;margin-top:16px;padding:10px 22px;background:#2949ff;color:#fff;border-radius:10px;text-decoration:none;font-weight:600}"
                 + "small{display:block;margin-top:20px;color:#8a93a8}</style></head><body><div class=\"box\">"
                 + "<h1>Có lỗi xảy ra</h1><p>Hệ thống chưa xử lý được yêu cầu của bạn. Dữ liệu của bạn vẫn an toàn.</p>"
                 + "<p>Vui lòng thử lại sau ít phút.</p><a href=\"/Home\">Về trang chủ</a>"
                 + "<small>Mã tra cứu: " + WebUtility.HtmlEncode(requestId) + "</small></div></body></html>";
        }
        #endregion
    }
}
