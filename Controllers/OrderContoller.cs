using Microsoft.AspNetCore.Mvc;
using Retail3.Services.Interface;
using Retail3.Models;

namespace Retail3.Controllers
{
    public class OrderController : Controller
    {
        private readonly IRetail3Service _retailService;

        public OrderController(IRetail3Service retailService)
        {
            _retailService = retailService;
        }

        // GET: /Order/
        public async Task<IActionResult> Index()
        {
            var orders = await _retailService.GetCustomerBoughtAsync();
            return View(orders);
        }

        // GET: /Order/Details/{id}
        public async Task<IActionResult> Details(string id)
        {
            var order = await _retailService.GetCustomerBoughtAsync();
            var selectedOrder = order.FirstOrDefault(o => o.RowKey == id);
            if (selectedOrder == null)
            {
                return NotFound();
            }
            return View(selectedOrder);
        }

        // POST: /Order/Buy
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Buy(string customerRowKey, string productRowKey)
        {
            if (string.IsNullOrEmpty(customerRowKey) || string.IsNullOrEmpty(productRowKey))
            {
                return BadRequest();
            }

            var order = await _retailService.BuyProductAsync(customerRowKey, productRowKey);
            return RedirectToAction(nameof(Details), new { id = order.RowKey });
        }

        // POST: /Order/Return/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Return(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            var order = await _retailService.ReturnProductAsync(id);
            return RedirectToAction(nameof(Details), new { id = order.RowKey });
        }
    }
}
