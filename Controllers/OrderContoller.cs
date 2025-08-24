using Microsoft.AspNetCore.Mvc;

namespace Retail3.Controllers
{
    public class OrderContoller : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
