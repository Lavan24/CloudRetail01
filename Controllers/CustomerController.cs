using Microsoft.AspNetCore.Mvc;

namespace Retail3.Controllers
{
    public class CustomerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
