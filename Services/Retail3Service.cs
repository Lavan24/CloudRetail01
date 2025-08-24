using Retail3.Services.Interface;
using Retail3.Services.Storage;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Retail3.Models;

namespace Retail3.Services
{
    public class Retail3Service : IRetail3Service
    {
        // -------------------- Table and storage names --------------------
        private const string CustomersTable = "customers";
        private const string ProdcutsTable = "products";
        private const string OrdersTable = "orders";
        private const string CoversContainer = "productimages"; // For book cover images
        private const string ActivityQueue = "retailactivities"; // For tracking activity messages
        private const string PoliciesShare = "documents"; // For policy documents

        // -------------------- Dependencies --------------------
        private readonly ITableStorageService _tableStorage; // Handles table storage operations
        private readonly IBlobStorageService _blobStorage;   // Handles blob storage operations
        private readonly IQueueService _queueService;        // Handles Azure queue messages
        private readonly IFileStorageService _fileStorage;   // Handles file storage (documents, etc.)

        // Constructor injects dependencies via DI
        public Retail3Service(
            ITableStorageService tableStorage,
            IBlobStorageService blobStorage,
            IQueueService queueService,
            IFileStorageService fileStorage)
        {
            _tableStorage = tableStorage;
            _blobStorage = blobStorage;
            _queueService = queueService;
            _fileStorage = fileStorage;
        }

        // -------------------- CUSTOMERS --------------------

        // Add a new customer
        public async Task<Customer> AddCustomerAsync(Customer customer)
        {
            customer.PartitionKey = "customers"; // Set a fixed partition key for all customers
            customer.RowKey = Guid.NewGuid().ToString(); // Assign unique ID
            await _tableStorage.AddEntityAsync(CustomersTable, customer); // Save to table storage
            await SendActivityMessageAsync($"New customer registered: {customer.FullName}"); // Log activity
            return customer;
        }


        // Get all customers
        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            return await _tableStorage.QueryEntitiesAsync<Customer>(
                CustomersTable,
                $"PartitionKey eq 'customers'");
        }

        // Get a single customer by RowKey
        public async Task<Customer?> GetCustomerAsync(string rowKey)
        {
            try
            {
                return await _tableStorage.GetEntityAsync<Customer>(
                    CustomersTable, "customers", rowKey);
            }
            catch
            {
                return null; // If not found, return null
            }
        }

        // -------------------- PRODUCTS --------------------

        // Add a new product
        public async Task<Product> AddProductAsync(Product product, IFormFile? productImage)
        {
            if (productImage != null && productImage.Length > 0)
            {
                // Upload product image to blob storage and store URL
                product.productImageURL = await _blobStorage.UploadFileAsync(CoversContainer, productImage);
            }

            product.RowKey = Guid.NewGuid().ToString(); // Unique product ID
            await _tableStorage.AddEntityAsync(ProdcutsTable, product); // Save product to table
            await SendActivityMessageAsync($"New product added: '{product.ProductName}'"); // Log activity
            return product;
        }

        // Get all products
        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _tableStorage.QueryEntitiesAsync<Product>(
                ProdcutsTable,
                $"PartitionKey eq 'products'");
        }

        // Get a single product by RowKey
        public async Task<Product?> GetProductAsync(string rowKey)
        {
            try
            {
                return await _tableStorage.GetEntityAsync<Product>(
                    ProdcutsTable, "products", rowKey);
            }
            catch
            {
                return null;
            }
        }

        // Update an existing product
        public async Task UpdateProductAsync(Product product, IFormFile? productImage)
        {
            if (string.IsNullOrEmpty(product.RowKey))
                throw new ArgumentException("Product ID is required");

            var existingProduct = await _tableStorage.GetEntityAsync<Product>(ProdcutsTable, "products", product.RowKey);

            // Update fields
            existingProduct.ProductName = product.ProductName;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.StockQuantity = product.StockQuantity;
            existingProduct.Category = product.Category;

            if (productImage != null && productImage.Length > 0)
                existingProduct.productImageURL = await _blobStorage.UploadFileAsync(CoversContainer, productImage);

            await _tableStorage.UpdateEntityAsync(ProdcutsTable, existingProduct);

            await SendActivityMessageAsync($"Product updated: '{product.ProductName}'");
        }

        // Delete a product
        public async Task DeleteProductAsync(string rowKey)
        {
            var product = await GetProductAsync(rowKey);
            if (product == null)
                throw new ArgumentException("Product not found");

            // Delete product image if it exists
            if (!string.IsNullOrEmpty(product.productImageURL))
            {
                var fileName = Path.GetFileName(new Uri(product.productImageURL).LocalPath);
                await _blobStorage.DeleteFileAsync(CoversContainer, fileName);
            }

            // Delete the product record
            await _tableStorage.DeleteEntityAsync(ProdcutsTable, "products", rowKey);
            await SendActivityMessageAsync($"Product deleted: '{product.ProductName}'");
        }

        // -------------------- ORDERS --------------------

        // Place an order
        public async Task<Order> BuyProductAsync(string customerRowKey, string productRowKey)
        {
            var customer = await GetCustomerAsync(customerRowKey);
            var product = await GetProductAsync(productRowKey);

            if (customer == null || product == null || product.StockQuantity <= 0)
                throw new InvalidOperationException("Invalid product or customer");

            var order = new Order
            {
                RowKey = Guid.NewGuid().ToString(),
                CustomerRowKey = customerRowKey,
                ProductRowKey = productRowKey,
                OrderDate = DateTime.UtcNow,
                TotalAmount = product.Price,
                Status = "Completed",
                FullName = customer.FullName,
                ProductName = product.ProductName,
                Price = product.Price.ToString("C")
            };

            await _tableStorage.AddEntityAsync(OrdersTable, order);

            // Reduce stock quantity
            product.StockQuantity--;
            await _tableStorage.UpdateEntityAsync(ProdcutsTable, product);

            await SendActivityMessageAsync(
                $"Order placed: {customer.FullName} bought '{product.ProductName}'");

            return order;
        }

        // Return a product
        public async Task<Order> ReturnProductAsync(string orderRowKey)
        {
            var order = await _tableStorage.GetEntityAsync<Order>(OrdersTable, "orders", orderRowKey);

            if (order.Status != "Completed")
                throw new InvalidOperationException("Order is not active");

            // Update order status
            order.Status = "Returned";
            order.Timestamp = DateTime.UtcNow;

            await _tableStorage.UpdateEntityAsync(OrdersTable, order);

            // Update product stock
            var product = await GetProductAsync(order.ProductRowKey);
            if (product != null)
            {
                product.StockQuantity++;
                await _tableStorage.UpdateEntityAsync(ProdcutsTable, product);
            }

            await SendActivityMessageAsync(
                $"Product returned: '{order.ProductName}' by {order.FullName}");

            return order;
        }

        // Get all active orders
        public async Task<List<Order>> GetCustomerBoughtAsync()
        {
            return await _tableStorage.QueryEntitiesAsync<Order>(
                OrdersTable,
                $"PartitionKey eq 'orders' and Status eq 'Completed'");
        }

        // Get all overdue orders (if applicable)
        public async Task<List<Order>> GetOverdueOrdersAsync()
        {
            var orders = await _tableStorage.QueryEntitiesAsync<Order>(
                OrdersTable,
                $"PartitionKey eq 'orders' and Status eq 'Completed'");

            // Assuming overdue logic is based on a specific condition
            return orders.Where(o => o.OrderDate.AddDays(30) < DateTime.UtcNow).ToList();
        }

        // -------------------- ACTIVITY TRACKING --------------------

        // Sends a message to track activities (e.g., "Product purchased by John")
        public async Task SendActivityMessageAsync(string message)
        {
            try
            {
                await _queueService.SendMessageAsync(ActivityQueue, message);
            }
            catch
            {
                // Failures here are non-critical
            }
        }

        // Retrieves the most recent activity messages
        public async Task<List<string>> GetRecentActivitiesAsync(int maxMessages = 10)
        {
            try
            {
                var messages = await _queueService.PeekMessagesAsync(ActivityQueue, maxMessages);

                // Parse JSON messages for display
                return messages.Select(m =>
                {
                    try
                    {
                        var json = JsonDocument.Parse(m);
                        return $"[{json.RootElement.GetProperty("Timestamp").GetString()}] " +
                               $"{json.RootElement.GetProperty("Message").GetString()}";
                    }
                    catch
                    {
                        return m;
                    }
                }).ToList();
            }
            catch
            {
                return new List<string> { "Failed to load activities" };
            }
        }

        // -------------------- DOCUMENT MANAGEMENT --------------------

        // Uploads a document (e.g., policy file)
        public async Task UploadDocumentsAsync(IFormFile policyFile)
        {
            await _fileStorage.UploadFileAsync(PoliciesShare, policyFile);
            await SendActivityMessageAsync($"Document uploaded: {policyFile.FileName}");
        }

        // Gets the list of uploaded document file names
        public async Task<List<string>> GetDocumentsAsync()
        {
            return await _fileStorage.ListFilesAsync(PoliciesShare);
        }

        // Downloads a document by its name
        public async Task<Stream> DownloadDocumentsAsync(string fileName)
        {
            return await _fileStorage.DownloadFileAsync(PoliciesShare, fileName);
        }

        // -------------------- DASHBOARD --------------------

        // Retrieves summary statistics for the dashboard
        public async Task<object> GetDashboardStatsAsync()
        {
            var customers = await GetAllCustomersAsync();
            var products = await GetAllProductsAsync();
            var orders = await GetCustomerBoughtAsync();

            return new
            {
                TotalCustomers = customers.Count,
                TotalProducts = products.Count,
                TotalOrders = orders.Count,
                AvailableProducts = products.Sum(p => p.StockQuantity),
                TotalRevenue = orders.Sum(o => o.TotalAmount)
            };
        }
    }
}
