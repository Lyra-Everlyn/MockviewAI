using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MockviewAI.Models.ViewModels;
using MockviewAI.Services.Profiles;

namespace MockviewAI.Controllers
{
    /// <summary>
    /// [PROFILE] 
    /// Xem và chỉnh sửa hồ sơ cá nhân (/Profile, /Profile/Edit).
    /// </summary>
    [Authorize]
    [Route("Profile")]
    public class ProfileController : Controller
    {
        #region Fields
        private readonly IProfileService _profileService;
        #endregion

        #region Constructor
        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }
        #endregion

        private string CurrentEmail => User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        #region Xem hồ sơ
        [HttpGet("")]
        public async Task<IActionResult> ViewProfile()
        {
            var profile = await _profileService.GetProfileAsync(CurrentEmail);
            if (profile == null) return RedirectToAction("Login", "Auth");

            return View(profile);
        }
        #endregion

        #region Chỉnh sửa hồ sơ
        [HttpGet("Edit")]
        public async Task<IActionResult> EditProfile()
        {
            var model = await _profileService.GetEditAsync(CurrentEmail);
            if (model == null) return RedirectToAction("Login", "Auth");

            return View(model);
        }

        [HttpPost("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileEditViewModel model)
        {
            if (!ModelState.IsValid) return View("EditProfile", model);

            var result = await _profileService.UpdateAsync(CurrentEmail, model);
            if (result == null) return RedirectToAction("Login", "Auth");

            await RefreshClaimsAsync(result.Value.FullName, result.Value.AvatarUrl);
            TempData["SuccessMessage"] = "Đã lưu hồ sơ.";

            return RedirectToAction("ViewProfile");
        }
        #endregion

        #region Helpers
        /// <summary>Đăng nhập lại cookie với tên và ảnh đại diện mới để thanh điều hướng cập nhật ngay.</summary>
        private async Task RefreshClaimsAsync(string fullName, string? avatarUrl)
        {
            var claims = User.Claims.Where(c => c.Type != ClaimTypes.Name && c.Type != "AvatarUrl").ToList();

            claims.Add(new Claim(ClaimTypes.Name, fullName));
            if (!string.IsNullOrEmpty(avatarUrl))
            {
                claims.Add(new Claim("AvatarUrl", avatarUrl));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        }
        #endregion
    }
}
