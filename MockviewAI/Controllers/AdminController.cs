using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MockviewAI.Services.Admin;

namespace MockviewAI.Controllers
{
    /// <summary>
    /// [ADMIN] 
    /// Trang quản trị CHỈ XEM: Tổng quan, Người dùng, Góp ý.
    /// Chỉ email khai báo ở Admin__Emails__N vào được (chính sách AdminOnly); người khác bị chuyển về trang chủ.
    /// </summary>
    [Authorize(Policy = AdminAccess.PolicyName)]
    [Route("Admin")]
    public class AdminController : Controller
    {
        #region Fields
        private readonly IAdminService _admin;
        #endregion

        #region Constructor
        public AdminController(IAdminService admin)
        {
            _admin = admin;
        }
        #endregion

        #region Pages
        /// <summary>GET: /Admin</summary>
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            SetTab("dashboard");
            return View(await _admin.GetDashboardAsync());
        }

        /// <summary>GET: /Admin/Users?search=&amp;status=&amp;page=</summary>
        [HttpGet("Users")]
        public async Task<IActionResult> Users(string? search, string? status, int page = 1)
        {
            SetTab("users");
            return View(await _admin.GetUsersAsync(search, status, page));
        }

        /// <summary>GET: /Admin/Feedbacks?rating=&amp;withComment=&amp;page=</summary>
        [HttpGet("Feedbacks")]
        public async Task<IActionResult> Feedbacks(int? rating, bool withComment = false, int page = 1)
        {
            SetTab("feedbacks");
            return View(await _admin.GetFeedbacksAsync(rating, withComment, page));
        }
        #endregion

        #region Helpers
        private void SetTab(string tab)
        {
            ViewData["ActiveNav"] = "admin";
            ViewData["AdminTab"] = tab;
        }
        #endregion
    }
}
