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

        // GET: /Customer/Details/{id}
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Customer ID is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var customer = await _retailService.GetCustomerAsync(id);
                if (customer == null)
                {
                    TempData["Error"] = "Customer not found.";
                    return RedirectToAction(nameof(Index));
                }
                return View(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer details for ID: {Id}", id);
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

        // GET: /Customer/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Customer ID is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var customer = await _retailService.GetCustomerAsync(id);
                if (customer == null)
                {
                    TempData["Error"] = "Customer not found.";
                    return RedirectToAction(nameof(Index));
                }
                return View(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer for edit: {Id}", id);
                TempData["Error"] = "Error loading customer for editing.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Customer/Delete/{id}
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Customer ID is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var customer = await _retailService.GetCustomerAsync(id);
                if (customer == null)
                {
                    TempData["Error"] = "Customer not found.";
                    return RedirectToAction(nameof(Index));
                }
                return View(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer for deletion: {Id}", id);
                TempData["Error"] = "Error loading customer for deletion.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
