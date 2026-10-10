using Microsoft.AspNetCore.Mvc;
using MockviewAI.Models;
using MockviewAI.Models.ViewModels;
using System.Diagnostics;
using System.Security.Claims;

namespace MockviewAI.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var viewModel = new HomeViewModel();
            var hour = DateTime.Now.Hour;

            if (hour >= 5 && hour < 12)
                viewModel.GreetingSub = "Chào buổi sáng";
            else if (hour >= 12 && hour < 18)
                viewModel.GreetingSub = "Chào buổi chiều";
            else
                viewModel.GreetingSub = "Chào buổi tối";

            // 2. Kiểm tra trạng thái đăng nhập
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                viewModel.IsAuthenticated = true;
                string fullName = User.FindFirstValue(ClaimTypes.Name) ?? "Người dùng";
                viewModel.FullName = fullName;

                viewModel.AvatarInitial = !string.IsNullOrWhiteSpace(fullName)
                    ? fullName.Substring(0, 1).ToUpper()
                    : "U";
                
                viewModel.AvatarUrl = User.FindFirstValue("AvatarUrl");

                // TODO: Sau này sẽ gọi Database (Service/Repository) để lấy dữ liệu thật
                // Tạm thời giả lập dữ liệu cho User đã đăng nhập
                viewModel.RecentSessions = new List<RecentSessionViewModel>
                {
                    new RecentSessionViewModel { RoleName = "Lập trình viên", DateFormatted = "06/10", Score = 78, AnxietyText = "Lo lắng 7 → 4" },
                    new RecentSessionViewModel { RoleName = "Phân tích dữ liệu", DateFormatted = "05/10", Score = 71, AnxietyText = "Lo lắng 8 → 6" }
                };
            }
            else
            {
                // Xử lý cho Khách (Guest)
                viewModel.IsAuthenticated = false;
                viewModel.FullName = "Khách";
                viewModel.AvatarInitial = "G";
                viewModel.RecentSessions = new List<RecentSessionViewModel>(); // Thông số trống
            }

            return View(viewModel);

        }






        public IActionResult Privacy()
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
