using System.Security.Claims;

namespace MockviewAI.Services.Admin
{
    /// <summary>
    /// [ADMIN] 
    /// Quyết định ai được vào /Admin. Dùng cho chính sách AdminOnly và link "Quản trị" trên thanh điều hướng.
    /// </summary>
    public interface IAdminAccess
    {
        /// <summary>true khi người dùng đã đăng nhập và email nằm trong Admin:Emails.</summary>
        bool IsAdmin(ClaimsPrincipal user);
    }
}
