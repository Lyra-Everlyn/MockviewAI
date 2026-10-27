using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MockviewAI.Models.ViewModels;
using MockviewAI.Services.Profiles;

namespace MockviewAI.Controllers
{
    /// [PROFILE]
    
    [Authorize]
    [Route("Profile")]
    public class ProfileController : Controller
    {
        private readonly IProfileService _profileService;

        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        private string CurrentEmail => User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        // GET: /Profile
        [HttpGet("")]
        public async Task<IActionResult> ViewProfile()
        {
            var profile = await _profileService.GetProfileAsync(CurrentEmail);
            if (profile == null) return RedirectToAction("Login", "Auth");

            return View(profile);
        }

        // GET: /Profile/Edit
        [HttpGet("Edit")]
        public async Task<IActionResult> EditProfile()
        {
            var model = await _profileService.GetEditAsync(CurrentEmail);
            if (model == null) return RedirectToAction("Login", "Auth");

            return View(model);
        }

        // POST: /Profile/Edit
        [HttpPost("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileEditViewModel model)
        {
            if (!ModelState.IsValid) return View("EditProfile", model);

            var newName = await _profileService.UpdateAsync(CurrentEmail, model);
            if (newName == null) return RedirectToAction("Login", "Auth");

            await RefreshNameClaimAsync(newName);
            TempData["SuccessMessage"] = "Đã lưu hồ sơ.";

            return RedirectToAction("ViewProfile");
        }

        private async Task RefreshNameClaimAsync(string fullName)
        {
            var claims = User.Claims.Where(c => c.Type != ClaimTypes.Name).ToList();
            claims.Add(new Claim(ClaimTypes.Name, fullName));
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        }
    }
}