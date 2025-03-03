using Microsoft.AspNetCore.Mvc;

namespace BuildWeek4.Controllers
{
    public class DetailsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
