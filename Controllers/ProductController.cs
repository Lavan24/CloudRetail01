using Microsoft.AspNetCore.Mvc;
using Retail3.Services.Interface;
using Retail3.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Retail3.Controllers
{
    public class ProductController : Controller
    {
        private readonly IRetail3Service _retailService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IRetail3Service retailService, ILogger<ProductController> logger)
        {
            _retailService = retailService;
            _logger = logger;
        }

        // GET: /Product/
        public async Task<IActionResult> Index()
        {
            try
            {
                var products = await _retailService.GetAllProductsAsync();
                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                TempData["Error"] = "Error loading product inventory. Please try again.";
                return View(new List<Product>());
            }
        }

        // GET: /Product/Details/{id}
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Product ID is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var product = await _retailService.GetProductAsync(id);
                if (product == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction(nameof(Index));
                }
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product details for ID: {Id}", id);
                TempData["Error"] = "Error loading product details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Product/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile productImage)
        {
            if (!ModelState.IsValid)
            {
                // Log validation errors for debugging
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("Validation error: {ErrorMessage}", error.ErrorMessage);
                }

                TempData["Error"] = "Please correct the validation errors below.";
                return View(product);
            }

            try
            {
                // Set Azure Table properties (in case they're not set by the service)
                product.PartitionKey = "products";
                product.RowKey = Guid.NewGuid().ToString();

                await _retailService.AddProductAsync(product, productImage);

                TempData["Success"] = $"Product '{product.ProductName}' created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product: {ProductName}", product.ProductName);
                TempData["Error"] = $"Error creating product: {ex.Message}";
                return View(product);
            }
        }

        // GET: /Product/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Product ID is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var product = await _retailService.GetProductAsync(id);
                if (product == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction(nameof(Index));
                }
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product for edit: {Id}", id);
                TempData["Error"] = "Error loading product for editing.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Product/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Product product, IFormFile productImage)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Product ID is required.";
                return RedirectToAction(nameof(Index));
            }

            if (id != product.RowKey)
            {
                TempData["Error"] = "Product ID mismatch.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please correct the validation errors below.";
                return View(product);
            }

            try
            {
                // Ensure partition key is set
                product.PartitionKey = "products";

                await _retailService.UpdateProductAsync(product, productImage);

                TempData["Success"] = $"Product '{product.ProductName}' updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product: {Id}", id);
                TempData["Error"] = $"Error updating product: {ex.Message}";
                return View(product);
            }
        }

        // GET: /Product/Delete/{id} - Added GET method for confirmation page
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Product ID is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var product = await _retailService.GetProductAsync(id);
                if (product == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction(nameof(Index));
                }
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product for deletion: {Id}", id);
                TempData["Error"] = "Error loading product for deletion.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Product/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Product ID is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _retailService.DeleteProductAsync(id);

                TempData["Success"] = "Product deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product: {Id}", id);
                TempData["Error"] = $"Error deleting product: {ex.Message}";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }
    }
}
