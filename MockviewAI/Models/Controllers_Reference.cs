using Microsoft.AspNetCore.Mvc;
using MockviewAI.ViewModels;

namespace MockviewAI.Controllers
{
    // 1. SessionController: Phụ trách tạo buổi luyện, kết quả, xem lại
    public class SessionController : Controller
    {
        // GET: /Session/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Session/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string position, string interviewer, string responseMode)
        {
            string newSessionId = "1";
            return RedirectToAction("Room", new { id = newSessionId });
        }

        // GET: /Session/Result/{id}
        [HttpGet]
        public IActionResult Result(string id)
        {
            var model = new SessionResultViewModel
            {
                SessionId = id ?? "1",
                ContentScore = 88,
                DeliveryScore = 80,
                AnxietyBefore = 7,
                AnxietyAfter = 3
            };
            return View(model);
        }

        // GET: /Session/Review/{id}
        [HttpGet]
        public IActionResult Review(string id)
        {
            var model = new SessionResultViewModel
            {
                SessionId = id ?? "1",
                ContentScore = 88,
                DeliveryScore = 80,
                AnxietyBefore = 7,
                AnxietyAfter = 3
            };
            return View(model);
        }

        // GET: /Session/History
        [HttpGet]
        public IActionResult History()
        {
            return View();
        }
    }

    // 2. ProfileController: Phụ trách trang cá nhân và sửa hồ sơ
    public class ProfileController : Controller
    {
        // GET: /Profile
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Profile/Edit
        [HttpGet]
        public IActionResult Edit()
        {
            var model = new ProfileEditViewModel
            {
                FullName = "Hải Anh",
                Major = "it",
                Position = "developer",
                StudentStatus = "graduating"
            };
            return View(model);
        }

        // POST: /Profile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ProfileEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            // TODO: Cập nhật thông tin người dùng vào CSDL
            TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công!";
            return RedirectToAction("Index");
        }
    }
}
