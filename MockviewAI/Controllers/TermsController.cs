using Microsoft.AspNetCore.Mvc;

namespace MockviewAI
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Terms()
        {
            return View();
        }
    }
}