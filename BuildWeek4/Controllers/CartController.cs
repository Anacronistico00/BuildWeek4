using Microsoft.AspNetCore.Mvc;

namespace BuildWeek4.Controllers
{
    public class CartController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
