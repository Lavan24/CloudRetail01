using Microsoft.AspNetCore.Mvc;

namespace Retail3.Controllers
{
    public class ProductController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
