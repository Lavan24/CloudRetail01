using Microsoft.AspNetCore.Mvc;
using Retail3.Services.Interface;
using Retail3.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Retail3.Controllers
{
    public class OrderController : Controller
    {
        private readonly IRetail3Service _retailService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IRetail3Service retailService, ILogger<OrderController> logger)
        {
            _retailService = retailService;
            _logger = logger;
        }

        // GET: /Order/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var customers = await _retailService.GetAllCustomersAsync();
                var products = await _retailService.GetAllProductsAsync();
                ViewBag.Customers = customers;
                ViewBag.Products = products;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create order page");
                TempData["Error"] = "Error loading order form. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Order/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order order)
        {
            try
            {
                // Reload dropdown data in case of validation errors
                var customers = await _retailService.GetAllCustomersAsync();
                var products = await _retailService.GetAllProductsAsync();
                ViewBag.Customers = customers;
                ViewBag.Products = products;

                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Please correct the validation errors below.";
                    return View(order);
                }

                // Get customer and product details for denormalized data
                var customer = await _retailService.GetCustomerAsync(order.CustomerRowKey);
                var product = await _retailService.GetProductAsync(order.ProductRowKey);

                if (customer == null || product == null)
                {
                    TempData["Error"] = "Invalid customer or product selected.";
                    return View(order);
                }

                // Set denormalized data
                order.FullName = customer.FullName;
                order.ProductName = product.ProductName;
                order.Price = product.Price.ToString("C");
                order.Status = "Pending"; // Default status

                // Use your BuyProductAsync method or create a new order directly
                var createdOrder = await _retailService.BuyProductAsync(order.CustomerRowKey, order.ProductRowKey);

                TempData["Success"] = $"Order created successfully for {customer.FullName}!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                TempData["Error"] = $"Error creating order: {ex.Message}";

                // Reload dropdown data
                var customers = await _retailService.GetAllCustomersAsync();
                var products = await _retailService.GetAllProductsAsync();
                ViewBag.Customers = customers;
                ViewBag.Products = products;

                return View(order);
            }
        }

        // GET: /Order/
        public async Task<IActionResult> Index()
        {
            try
            {
                var orders = await _retailService.GetCustomerBoughtAsync();
                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders");
                TempData["Error"] = "Error loading orders. Please try again.";
                return View(new List<Order>());
            }
        }

        // GET: /Order/Details/{id}
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Order ID is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var orders = await _retailService.GetCustomerBoughtAsync();
                var selectedOrder = orders.FirstOrDefault(o => o.RowKey == id);

                if (selectedOrder == null)
                {
                    TempData["Error"] = "Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(selectedOrder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order details for ID: {Id}", id);
                TempData["Error"] = "Error loading order details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Order/Return/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Return(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Order ID is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var order = await _retailService.ReturnProductAsync(id);
                TempData["Success"] = "Product returned successfully!";
                return RedirectToAction(nameof(Details), new { id = order.RowKey });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error returning product for order ID: {Id}", id);
                TempData["Error"] = $"Error returning product: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}