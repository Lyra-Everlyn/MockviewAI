using Microsoft.AspNetCore.Authorization;

namespace MockviewAI.Services.Admin
{
    /// <summary>
    /// [ADMIN] 
    /// Yêu cầu của chính sách AdminOnly: người dùng phải có email trong Admin:Emails.
    /// </summary>
    public class AdminRequirement : IAuthorizationRequirement
    {
    }
}
