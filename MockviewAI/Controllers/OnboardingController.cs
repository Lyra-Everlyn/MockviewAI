using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MockviewAI.Models.DTOs;
using MockviewAI.Services.Onboarding;

namespace MockviewAI.Controllers
{
    /// <summary>
    /// [ONBOARDING] 
    /// Thiết lập hồ sơ ban đầu ngay sau lần đăng nhập đầu tiên.
    /// </summary>
    [Authorize]
    public class OnboardingController : Controller
    {
        #region Fields
        private const string SessionExpiredMessage = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
        private const string SaveFailedMessage = "Không lưu được hồ sơ. Vui lòng thử lại sau ít phút.";

        private readonly IOnboardingService _onboardingService;
        private readonly ILogger<OnboardingController> _logger;
        #endregion

        #region Constructor
        public OnboardingController(IOnboardingService onboardingService, ILogger<OnboardingController> logger)
        {
            _onboardingService = onboardingService;
            _logger = logger;
        }
        #endregion

        #region Actions
        /// <summary>Đã thiết lập xong thì về trang chủ, chưa thì hiện form.</summary>
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

        /// <summary>Lưu hồ sơ (Onboarding.js gửi JSON). Trả { success, message } hoặc { success, redirectUrl }.</summary>
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
                return Json(new { success = false, message = SessionExpiredMessage });
            }

            try
            {
                bool saved = await _onboardingService.SaveOnboardingInfoAsync(userEmail, model);
                if (!saved)
                    return Json(new { success = false, message = SessionExpiredMessage });

                return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });
            }
            catch (Exception ex)
            {
                // Không trả nguyên văn lỗi hệ thống (database...) về trình duyệt
                _logger.LogError(ex, "Saving onboarding profile failed");
                return Json(new { success = false, message = SaveFailedMessage });
            }
        }
        #endregion
    }
}
