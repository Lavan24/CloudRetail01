using Microsoft.AspNetCore.Mvc;
using Retail3.Services.Interface;
using Retail3.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Retail3.Services;
using System.Globalization;

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
                var order = await _retailService.GetOrderAsync(id);

                if (order == null)
                {
                    TempData["Error"] = "Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order details for ID: {Id}", id);
                TempData["Error"] = "Error loading order details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Order/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                await LoadDropdownData();
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
                await LoadDropdownData();

                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Please correct the validation errors below.";
                    return View(order);
                }

                var createdOrder = await _retailService.BuyProductAsync(order.CustomerRowKey, order.ProductRowKey);

                TempData["Success"] = $"Order created successfully!";
                return RedirectToAction(nameof(Details), new { id = createdOrder.RowKey });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                TempData["Error"] = $"Error creating order: {ex.Message}";
                await LoadDropdownData();
                return View(order);
            }
        }

        // GET: /Order/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Order ID is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var order = await _retailService.GetOrderAsync(id);

                if (order == null)
                {
                    TempData["Error"] = "Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                await LoadDropdownData();
                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit page for order ID: {Id}", id);
                TempData["Error"] = "Error loading order edit page.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Order order)
        {
            if (id != order.RowKey)
            {
                TempData["Error"] = "Order ID mismatch.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                if (ModelState.IsValid)
                {
                    // ✅ Ensure OrderDate is UTC
                    if (order.OrderDate.Kind == DateTimeKind.Unspecified)
                    {
                        order.OrderDate = DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Utc);
                    }
                    else if (order.OrderDate.Kind == DateTimeKind.Local)
                    {
                        order.OrderDate = order.OrderDate.ToUniversalTime();
                    }

                    // ✅ Update TotalAmount based on selected product
                    var product = await _retailService.GetProductAsync(order.ProductRowKey);
                    if (product == null)
                    {
                        TempData["Error"] = "Product not found.";
                        await LoadDropdownData();
                        return View(order);
                    }

                    decimal priceDecimal = (decimal)product.Price;
                    order.TotalAmount = priceDecimal;
                    order.Price = priceDecimal.ToString("C", new CultureInfo("en-ZA"));
                    order.ProductName = product.ProductName;

                    // Update the order in storage
                    await _retailService.UpdateOrderAsync(order);

                    TempData["Success"] = "Order updated successfully!";
                    return RedirectToAction(nameof(Details), new { id = order.RowKey });
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error updating order: " + ex.Message;
            }

            // Reload dropdowns if validation fails or exception occurs
            ViewBag.Customers = await _retailService.GetAllCustomersAsync();
            ViewBag.Products = await _retailService.GetAllProductsAsync();

            return View(order);
        }



        // GET: /Order/Delete/{id}
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Order ID is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var order = await _retailService.GetOrderAsync(id);

                if (order == null)
                {
                    TempData["Error"] = "Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete page for order ID: {Id}", id);
                TempData["Error"] = "Error loading order delete page.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Order/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Order ID is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // PartitionKey is always "orders" in your service
                await _retailService.DeleteOrderAsync("orders", id);

                TempData["Success"] = "Order deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order ID: {Id}", id);
                TempData["Error"] = $"Error deleting order: {ex.Message}";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }


        // POST: /Order/DeleteConfirmed/{id}
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id, string partitionKey)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(partitionKey))
            {
                TempData["Error"] = "Order ID and Partition Key are required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _retailService.DeleteOrderAsync(partitionKey, id);
                TempData["Success"] = "Order deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order ID: {Id}", id);
                TempData["Error"] = $"Error deleting order: {ex.Message}";
                return RedirectToAction(nameof(Delete), new { id });
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

        // Helper method to load dropdown data
        private async Task LoadDropdownData()
        {
            var customers = await _retailService.GetAllCustomersAsync();
            var products = await _retailService.GetAllProductsAsync();

            // Pass the raw lists instead of SelectList
            ViewBag.Customers = customers;
            ViewBag.Products = products;
        }
    }
}