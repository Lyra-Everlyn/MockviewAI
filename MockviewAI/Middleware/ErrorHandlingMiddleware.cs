using System.Net;

namespace MockviewAI.Middleware
{
    public class ErrorHandlingMiddleware
    {
        /// <summary>
        /// [BẢN TEST] 
        /// -- Chốt chặn lỗi toàn ứng dụng: bắt mọi lỗi không lường trước ở MỘT chỗ,
        /// -- Ghi log, rồi trả trang/JSON thân thiện cho người dùng. Không để lỗi "văng" ra ngoài.
        /// </summary>

        private const string FriendlyMessage = "Có lỗi xảy ra. Vui lòng thử lại.";

        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

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

        // Các request gọi bằng JavaScript (fetch) cần JSON để hiện thông báo trong trang, không cần cả trang HTML
        private static bool WantsJson(HttpRequest request)
        {
            string contentType = request.ContentType ?? string.Empty;
            string accept = request.Headers.Accept.ToString();
            return contentType.Contains("json", StringComparison.OrdinalIgnoreCase)
                   || accept.Contains("application/json", StringComparison.OrdinalIgnoreCase);
        }

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
    }
}
