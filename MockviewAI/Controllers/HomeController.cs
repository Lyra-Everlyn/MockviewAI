using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MockviewAI.Models;
using MockviewAI.Models.DTOs;
using MockviewAI.Services.Feedbacks;
using MockviewAI.Services.SessionHistory;
using System.Diagnostics;
using System.Security.Claims;

namespace MockviewAI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ISessionHistoryService _history;
        private readonly IFeedbackService _feedback;

        public HomeController(ISessionHistoryService history, IFeedbackService feedback)
        {
            _history = history;
            _feedback = feedback;
        }

        private string CurrentEmail => User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        #region Trang chủ
        [Authorize]
        [Route("Home")]
        [Route("Home/Index")]
        [Route("")] // Gắn luôn route rỗng để truy cập root domain sẽ nhảy vào Home
        public async Task<IActionResult> Index()
        {
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