using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MockviewAI.Models;
using MockviewAI.Models.DTOs;
using MockviewAI.Models.ViewModels;
using MockviewAI.Services.Interfaces;
using System.Diagnostics;
using System.Security.Claims;

namespace MockviewAI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ISessionHistoryService _history;
        private readonly IProfileService _profile;
        private readonly IFeedbackService _feedback;

        public HomeController(ISessionHistoryService history, IProfileService profile, IFeedbackService feedback)
        {
            _history = history;
            _profile = profile;
            _feedback = feedback;
        }

        // Email của người đang đăng nhập (lấy từ cookie, không lấy từ dữ liệu gửi lên)
        private string CurrentEmail => User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        #region Trang chủ
        // Route mặc định của app là Auth/Login, nên phải khai báo rõ để "/Home" vào được trang chủ
        [Authorize]
        [Route("Home")]
        [Route("Home/Index")]
        public async Task<IActionResult> Index()
        {
            // 2 buổi luyện gần nhất lấy từ database (lời chào, tên lấy trong view từ cookie đăng nhập)
            var recent = await _history.GetHistoryAsync(CurrentEmail, 2);
            return View(recent);
        }






        public IActionResult Privacy()
        {
            return View();
        }
        #endregion

        #region Lịch sử + Xem lại hội thoại
        [Authorize]
        [Route("Home/History")]
        public async Task<IActionResult> History()
        {
            var sessions = await _history.GetHistoryAsync(CurrentEmail);
            return View(sessions);
        }

        [Authorize]
        [Route("Home/InterviewDetail/{id:int}")]
        public async Task<IActionResult> InterviewDetail(int id)
        {
            var detail = await _history.GetDetailAsync(CurrentEmail, id);
            if (detail == null) return NotFound();
            return View(detail);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Home/DeleteSession/{id:int}")]
        public async Task<IActionResult> DeleteSession(int id)
        {
            bool deleted = await _history.DeleteAsync(CurrentEmail, id);
            if (deleted) TempData["SuccessMessage"] = "Đã xoá buổi luyện.";
            else TempData["ErrorMessage"] = "Không tìm thấy buổi luyện.";
            return RedirectToAction("History");
        }
        #endregion

        #region Hồ sơ
        [Authorize]
        [Route("Profile")]
        [Route("Home/Profile")]
        public async Task<IActionResult> Profile()
        {
            var profile = await _profile.GetProfileAsync(CurrentEmail);
            if (profile == null) return RedirectToAction("Login", "Auth");
            return View(profile);
        }

        [Authorize]
        [HttpGet]
        [Route("Home/EditProfile")]
        public async Task<IActionResult> EditProfile()
        {
            var model = await _profile.GetEditAsync(CurrentEmail);
            if (model == null) return RedirectToAction("Login", "Auth");
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Home/EditProfile")]
        public async Task<IActionResult> EditProfile(ProfileEditViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var newName = await _profile.UpdateAsync(CurrentEmail, model);
            if (newName == null) return RedirectToAction("Login", "Auth");

            await RefreshNameClaimAsync(newName);
            TempData["SuccessMessage"] = "Đã lưu hồ sơ.";
            return RedirectToAction("Profile");
        }

        // Cập nhật tên trong cookie đăng nhập để trang chủ hiện tên mới ngay
        private async Task RefreshNameClaimAsync(string fullName)
        {
            var claims = User.Claims.Where(c => c.Type != ClaimTypes.Name).ToList();
            claims.Add(new Claim(ClaimTypes.Name, fullName));
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        }
        #endregion

        #region Góp ý
        [Authorize]
        [HttpGet]
        [Route("Feedback")]
        [Route("Home/Feedback")]
        public IActionResult Feedback()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Feedback")]
        [Route("Home/Feedback")]
        public async Task<IActionResult> Feedback(FeedbackDto dto)
        {
            if (!ModelState.IsValid) return View();

            bool saved = await _feedback.SubmitAsync(CurrentEmail, dto);
            if (!saved) return RedirectToAction("Login", "Auth");
            return RedirectToAction("ThankYou");
        }

        [Authorize]
        public IActionResult ThankYou()
        {
            return View();
        }
        #endregion

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
