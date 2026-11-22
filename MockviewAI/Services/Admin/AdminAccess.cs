using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace MockviewAI.Services.Admin
{
    /// <summary>
    /// [ADMIN] 
    /// So email trong cookie đăng nhập với Admin:Emails (đọc một lần khi khởi động).
    /// Không đọc / ghi database; đổi danh sách = sửa biến môi trường rồi khởi động lại app.
    /// </summary>
    public class AdminAccess : IAdminAccess
    {
        #region Fields
        /// <summary>Tên chính sách dùng trong [Authorize(Policy = ...)].</summary>
        public const string PolicyName = "AdminOnly";

        private readonly HashSet<string> _emails;
        #endregion

        #region Constructor
        public AdminAccess(IOptions<AdminOptions> options)
        {
            _emails = options.Value.Emails
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim().ToLowerInvariant())
                .ToHashSet();
        }
        #endregion

        #region IAdminAccess
        public bool IsAdmin(ClaimsPrincipal user)
        {
            if (user.Identity?.IsAuthenticated != true || _emails.Count == 0) return false;

            var email = user.FindFirstValue(ClaimTypes.Email);
            return !string.IsNullOrWhiteSpace(email) && _emails.Contains(email.Trim().ToLowerInvariant());
        }
        #endregion
    }
}
