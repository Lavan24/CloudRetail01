using Microsoft.AspNetCore.Mvc;
using Retail3.Services.Interface;
using Retail3.Models;
using System.Diagnostics;
using Retail3.Services.Storage;

namespace Retail3.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IRetail3Service _retailService;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(IRetail3Service retailService, ILogger<CustomerController> logger)
        {
            _retailService = retailService;
            _logger = logger;
        }

        // GET: /Customer/
        public async Task<IActionResult> Index()
        {
            try
            {
                var customers = await _retailService.GetAllCustomersAsync();
                return View(customers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customers");
                TempData["Error"] = "Error loading customer list. Please try again.";
                return View(new List<Customer>());
            }
        }

        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey) || string.IsNullOrEmpty(partitionKey))
            {
                TempData["Error"] = "Customer ID is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var customer = await _retailService.GetCustomerAsync(rowKey);
                if (customer == null)
                {
                    TempData["Error"] = "Customer not found.";
                    return RedirectToAction(nameof(Index));
                }
                return View(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer details: {RowKey}", rowKey);
                TempData["Error"] = "Error loading customer details.";
                return RedirectToAction(nameof(Index));
            }
        }


        // GET: /Customer/Create
        public IActionResult Create()
        {
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                _logger.LogWarning("Validation error: {ErrorMessage}", error.ErrorMessage);
            }
            return View();
        }

        // POST: /Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer, IFormFile IdImage, [FromServices] IFileStorageService fileStorageService)
        {
            // Handle file upload first
            if (IdImage != null && IdImage.Length > 0)
            {
                try
                {
                    string fileName = $"{Guid.NewGuid()}{Path.GetExtension(IdImage.FileName)}";
                    await fileStorageService.UploadFileAsync("documents", IdImage);
                    customer.IdImagePath = fileName;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading ID image");
                    TempData["Error"] = "Error uploading ID image. Please try again.";
                    return View(customer);
                }
            }

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("Validation error: {ErrorMessage}", error.ErrorMessage);
                }
                TempData["Error"] = "Please correct the validation errors below.";
                return View(customer);
            }

            try
            {
                // Save customer to database
                var createdCustomer = await _retailService.AddCustomerAsync(customer);

                TempData["Success"] = $"Customer '{createdCustomer.FullName}' created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer");
                TempData["Error"] = $"Error creating customer: {ex.Message}";
                return View(customer);
            }
        }

        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey) || string.IsNullOrEmpty(partitionKey))
            {
                TempData["Error"] = "Customer ID is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var customer = await _retailService.GetCustomerAsync(rowKey);
                if (customer == null)
                {
                    TempData["Error"] = "Customer not found.";
                    return RedirectToAction(nameof(Index));
                }
                return View(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer for edit: {RowKey}", rowKey);
                TempData["Error"] = "Error loading customer for editing.";
                return RedirectToAction(nameof(Index));
            }
        }


        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey) || string.IsNullOrEmpty(partitionKey))
            {
                TempData["Error"] = "Customer ID is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var customer = await _retailService.GetCustomerAsync(rowKey);
                if (customer == null)
                {
                    TempData["Error"] = "Customer not found.";
                    return RedirectToAction(nameof(Index));
                }
                return View(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer for deletion: {RowKey}", rowKey);
                TempData["Error"] = "Error loading customer for deletion.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey, Customer updatedCustomer, IFormFile? IdImage, [FromServices] IFileStorageService fileStorageService)
        {
            if (!ModelState.IsValid)
            {
                return View(updatedCustomer);
            }

            try
            {
                // Ensure correct keys
                updatedCustomer.PartitionKey = partitionKey;
                updatedCustomer.RowKey = rowKey;

                // Handle file upload
                if (IdImage != null && IdImage.Length > 0)
                {
                    string fileName = $"{Guid.NewGuid()}{Path.GetExtension(IdImage.FileName)}";
                    await fileStorageService.UploadFileAsync("documents", IdImage);
                    updatedCustomer.IdImagePath = fileName;
                }

                await _retailService.UpdateCustomerAsync(updatedCustomer);

                TempData["Success"] = "Customer updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer");
                TempData["Error"] = "Error updating customer.";
                return View(updatedCustomer);
            }
        }



        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string partitionKey, string rowKey)
        {
            try
            {
                var customer = await _retailService.GetCustomerAsync(rowKey);
                if (customer == null)
                {
                    TempData["Error"] = "Customer not found.";
                    return RedirectToAction(nameof(Index));
                }

                await _retailService.DeleteCustomerAsync(partitionKey, rowKey);  // ✅ use service method

                TempData["Success"] = $"Customer '{customer.FullName}' deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer: {Id}", rowKey);
                TempData["Error"] = "Error deleting customer.";
                return RedirectToAction(nameof(Index));
            }
        }


    }
}
