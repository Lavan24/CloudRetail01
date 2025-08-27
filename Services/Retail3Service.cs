using Retail3.Services.Interface;
using Retail3.Services.Storage;
using System.Globalization;
using System.Text.Json;
using Retail3.Models;
using Azure;
using Microsoft.AspNetCore.Http;

namespace Retail3.Services
{
    public class Retail3Service : IRetail3Service
    {
        // -------------------- Table and storage names --------------------
        private const string CustomersTable = "customers";
        private const string ProductsTable = "products";
        private const string OrdersTable = "orders";
        private const string CoversContainer = "productimages";
        private const string ActivityQueue = "retailactivities";
        private const string PoliciesShare = "documents";

        // -------------------- Dependencies --------------------
        private readonly ITableStorageService _tableStorage;
        private readonly IBlobStorageService _blobStorage;
        private readonly IQueueService _queueService;
        private readonly IFileStorageService _fileStorage;

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
        public async Task<Customer> AddCustomerAsync(Customer customer)
        {
            customer.PartitionKey = "customers";
            customer.RowKey = Guid.NewGuid().ToString();
            await _tableStorage.AddEntityAsync(CustomersTable, customer);
            await SendActivityMessageAsync($"New customer registered: {customer.FullName}");
            return customer;
        }

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            return await _tableStorage.QueryEntitiesAsync<Customer>(
                CustomersTable,
                $"PartitionKey eq 'customers'");
        }

        public async Task<Customer?> GetCustomerAsync(string rowKey)
        {
            try
            {
                return await _tableStorage.GetEntityAsync<Customer>(
                    CustomersTable, "customers", rowKey);
            }
            catch { return null; }
        }

        public async Task UpdateCustomerAsync(Customer customer)
        {
            await _tableStorage.UpdateEntityAsync(CustomersTable, customer);
            await SendActivityMessageAsync($"Customer updated: {customer.FullName}");
        }

        public async Task DeleteCustomerAsync(string partitionKey, string rowKey)
        {
            await _tableStorage.DeleteEntityAsync(CustomersTable, partitionKey, rowKey);
            await SendActivityMessageAsync($"Customer deleted: {rowKey}");
        }

        // -------------------- PRODUCTS --------------------
        public async Task<Product> AddProductAsync(Product product, IFormFile? productImage)
        {
            if (productImage != null && productImage.Length > 0)
                product.productImageURL = await _blobStorage.UploadFileAsync(CoversContainer, productImage);

            product.PartitionKey = "products";
            product.RowKey = Guid.NewGuid().ToString();
            await _tableStorage.AddEntityAsync(ProductsTable, product);
            await SendActivityMessageAsync($"New product added: '{product.ProductName}'");
            return product;
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _tableStorage.QueryEntitiesAsync<Product>(
                ProductsTable,
                $"PartitionKey eq 'products'");
        }

        public async Task<Product?> GetProductAsync(string rowKey)
        {
            try
            {
                return await _tableStorage.GetEntityAsync<Product>(
                    ProductsTable, "products", rowKey);
            }
            catch { return null; }
        }

        public async Task UpdateProductAsync(Product product, IFormFile? productImage)
        {
            if (string.IsNullOrEmpty(product.RowKey))
                throw new ArgumentException("Product ID is required");

            var existingProduct = await _tableStorage.GetEntityAsync<Product>(
                ProductsTable, "products", product.RowKey);

            // Update fields
            existingProduct.ProductName = product.ProductName;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.StockQuantity = product.StockQuantity;
            existingProduct.Category = product.Category;

            // Update image if provided
            if (productImage != null && productImage.Length > 0)
                existingProduct.productImageURL = await _blobStorage.UploadFileAsync(CoversContainer, productImage);

            // Set ETag from the posted model for concurrency
            existingProduct.ETag = product.ETag;

            await _tableStorage.UpdateEntityAsync(ProductsTable, existingProduct);
            await SendActivityMessageAsync($"Product updated: '{product.ProductName}'");
        }

        public async Task DeleteProductAsync(string rowKey)
        {
            var product = await GetProductAsync(rowKey);
            if (product == null)
                throw new ArgumentException("Product not found");

            if (!string.IsNullOrEmpty(product.productImageURL))
            {
                var fileName = Path.GetFileName(new Uri(product.productImageURL).LocalPath);
                await _blobStorage.DeleteFileAsync(CoversContainer, fileName);
            }

            await _tableStorage.DeleteEntityAsync(ProductsTable, "products", rowKey);
            await SendActivityMessageAsync($"Product deleted: '{product.ProductName}'");
        }

        // -------------------- ORDERS --------------------
        public async Task<Order> BuyProductAsync(string customerRowKey, string productRowKey)
        {
            var customer = await GetCustomerAsync(customerRowKey);
            var product = await GetProductAsync(productRowKey);

            if (customer == null || product == null || product.StockQuantity <= 0)
                throw new InvalidOperationException("Invalid product or customer");

            // Convert product.Price (double) to decimal for currency calculations
            decimal priceDecimal = Convert.ToDecimal(product.Price);

            var order = new Order
            {
                PartitionKey = "orders",
                RowKey = Guid.NewGuid().ToString(),
                CustomerRowKey = customerRowKey,
                ProductRowKey = productRowKey,
                OrderDate = DateTime.UtcNow,
                TotalAmount = priceDecimal,   // correctly saved as decimal
                Status = "Completed",
                FullName = customer.FullName,
                ProductName = product.ProductName,
                Price = priceDecimal.ToString("C", new CultureInfo("en-ZA")) // stored as formatted string (e.g. R200.00)
            };

            await _tableStorage.AddEntityAsync(OrdersTable, order);

            // reduce stock
            product.StockQuantity--;
            await _tableStorage.UpdateEntityAsync(ProductsTable, product);

            await SendActivityMessageAsync(
                $"Order placed: {customer.FullName} bought '{product.ProductName}' for {order.Price}");

            return order;
        }

        public async Task<Order> ReturnProductAsync(string orderRowKey)
        {
            var order = await _tableStorage.GetEntityAsync<Order>(OrdersTable, "orders", orderRowKey);

            if (order.Status != "Completed")
                throw new InvalidOperationException("Order is not active");

            order.Status = "Returned";
            order.Timestamp = DateTime.UtcNow;
            await _tableStorage.UpdateEntityAsync(OrdersTable, order);

            var product = await GetProductAsync(order.ProductRowKey);
            if (product != null)
            {
                product.StockQuantity++;
                await _tableStorage.UpdateEntityAsync(ProductsTable, product);
            }

            await SendActivityMessageAsync(
                $"Product returned: '{order.ProductName}' by {order.FullName}");

            return order;
        }

        public async Task<List<Order>> GetCustomerBoughtAsync()
        {
            return await _tableStorage.QueryEntitiesAsync<Order>(
                OrdersTable,
                $"PartitionKey eq 'orders'");
        }

        public async Task<Order?> GetOrderAsync(string rowKey)
        {
            try
            {
                return await _tableStorage.GetEntityAsync<Order>(
                    OrdersTable, "orders", rowKey);
            }
            catch { return null; }
        }

        public async Task UpdateOrderAsync(Order order)
        {
            if (string.IsNullOrEmpty(order.RowKey))
                throw new ArgumentException("Order ID is required");

            // Get customer and product details for denormalized data
            var customer = await GetCustomerAsync(order.CustomerRowKey);
            var product = await GetProductAsync(order.ProductRowKey);

            if (customer == null || product == null)
                throw new InvalidOperationException("Invalid customer or product selected");

            // Update denormalized data
            order.FullName = customer.FullName;
            order.ProductName = product.ProductName;
            order.Price = product.Price.ToString("C", new CultureInfo("en-ZA"));

            await _tableStorage.UpdateEntityAsync(OrdersTable, order);
            await SendActivityMessageAsync($"Order updated: {order.RowKey}");
        }

        public async Task DeleteOrderAsync(string partitionKey, string rowKey)
        {
            await _tableStorage.DeleteEntityAsync(OrdersTable, partitionKey, rowKey);
            await SendActivityMessageAsync($"Order deleted: {rowKey}");
        }

        public async Task<List<Order>> GetOverdueOrdersAsync()
        {
            var orders = await _tableStorage.QueryEntitiesAsync<Order>(
                OrdersTable,
                $"PartitionKey eq 'orders' and Status eq 'Completed'");

            return orders.Where(o => o.OrderDate.AddDays(30) < DateTime.UtcNow).ToList();
        }

        // -------------------- ACTIVITY TRACKING --------------------
        public async Task SendActivityMessageAsync(string message)
        {
            try
            {
                var activityMessage = new
                {
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    Message = message
                };
                await _queueService.SendMessageAsync(ActivityQueue, JsonSerializer.Serialize(activityMessage));
            }
            catch { }
        }

        public async Task<List<string>> GetRecentActivitiesAsync(int maxMessages = 10)
        {
            try
            {
                var messages = await _queueService.PeekMessagesAsync(ActivityQueue, maxMessages);
                return messages.Select(m =>
                {
                    try
                    {
                        var json = JsonDocument.Parse(m);
                        return $"[{json.RootElement.GetProperty("Timestamp").GetString()}] " +
                               $"{json.RootElement.GetProperty("Message").GetString()}";
                    }
                    catch { return m; }
                }).ToList();
            }
            catch
            {
                return new List<string> { "Failed to load activities" };
            }
        }

        // -------------------- DOCUMENT MANAGEMENT --------------------
        public async Task UploadDocumentsAsync(IFormFile policyFile)
        {
            await _fileStorage.UploadFileAsync(PoliciesShare, policyFile);
            await SendActivityMessageAsync($"Document uploaded: {policyFile.FileName}");
        }

        public async Task<List<string>> GetDocumentsAsync()
        {
            return await _fileStorage.ListFilesAsync(PoliciesShare);
        }

        public async Task<Stream> DownloadDocumentsAsync(string fileName)
        {
            return await _fileStorage.DownloadFileAsync(PoliciesShare, fileName);
        }

        // -------------------- DASHBOARD --------------------
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