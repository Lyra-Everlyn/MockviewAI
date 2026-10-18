using Microsoft.AspNetCore.Mvc;
using MockviewAI.Models;
using System.Diagnostics;

namespace MockviewAI.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // Các trang giao diện tĩnh (chưa có dữ liệu thật), chỉ trả về view có sẵn
        [Route("Home/History")]
        public IActionResult History()
        {
            return View();
        }

        [Route("Profile")]
        [Route("Home/Profile")]
        public IActionResult Profile()
        {
            return View();
        }

        [Route("Feedback")]
        [Route("Home/Feedback")]
        public IActionResult Feedback()
        {
            return View();
        }

        public IActionResult ThankYou()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
