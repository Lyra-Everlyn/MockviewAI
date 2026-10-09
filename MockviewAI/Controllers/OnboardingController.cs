using Microsoft.AspNetCore.Mvc;
using MockviewAI.Models.DTOs;
using MockviewAI.Services.Helper.Interfaces;
using System.Security.Claims;

namespace MockviewAI.Controllers
{
    public class OnboardingController : Controller
    {
        private readonly IOnboardingService _onboardingService;
        public OnboardingController(IOnboardingService onboardingService)
        {
            _onboardingService = onboardingService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            string? userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Auth");
            }

            bool isCompleted = await _onboardingService.IsOnboardingCompletedAsync(userEmail);
            if (isCompleted)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new OnboardingDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete([FromBody] OnboardingDto model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return Json(new { success = false, message = string.Join(" ", errors) });
            }

            string? userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(userEmail))
            {
                return Json(new { success = false, message = "Session expired. Please log in again." });
            }

            try
            {
                await _onboardingService.SaveOnboardingInfoAsync(userEmail, model);
                return Json(new { success = true, redirectUrl = Url.Action("Index", "User") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
