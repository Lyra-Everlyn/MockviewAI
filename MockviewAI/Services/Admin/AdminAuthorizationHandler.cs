using Microsoft.AspNetCore.Authorization;

namespace MockviewAI.Services.Admin
{
    /// <summary>
    /// [ADMIN] 
    /// Xét AdminRequirement bằng IAdminAccess. Không đạt: chưa đăng nhập -> trang đăng nhập,
    /// đã đăng nhập nhưng không phải admin -> AccessDeniedPath (trang chủ).
    /// </summary>
    public class AdminAuthorizationHandler : AuthorizationHandler<AdminRequirement>
    {
        #region Fields
        private readonly IAdminAccess _access;
        #endregion

        #region Constructor
        public AdminAuthorizationHandler(IAdminAccess access)
        {
            _access = access;
        }
        #endregion

        #region AuthorizationHandler
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminRequirement requirement)
        {
            if (_access.IsAdmin(context.User))
                context.Succeed(requirement);
            return Task.CompletedTask;
        }
        #endregion
    }
}
