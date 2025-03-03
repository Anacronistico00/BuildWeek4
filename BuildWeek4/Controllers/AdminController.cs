using Microsoft.AspNetCore.Mvc;

namespace BuildWeek4.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
