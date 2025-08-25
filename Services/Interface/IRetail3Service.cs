using Retail3.Models;

namespace Retail3.Services.Interface
{
    public interface IRetail3Service
    {
        // =====================
        // Customer operations
        // =====================
        Task<Customer> AddCustomerAsync(Customer customer);
        Task<List<Customer>> GetAllCustomersAsync();
        Task<Customer?> GetCustomerAsync(string rowKey);
        Task UpdateCustomerAsync(Customer customer);
        Task DeleteCustomerAsync(string partitionKey, string rowKey);


        // =====================
        // Product operations
        // =====================
        Task<Product> AddProductAsync(Product product, IFormFile? productImage);
        Task<List<Product>> GetAllProductsAsync();
        Task<Product?> GetProductAsync(string rowKey);
        Task UpdateProductAsync(Product product, IFormFile? productImage);
        Task DeleteProductAsync(string rowKey);

        // =====================
        // Order operations
        // =====================
        Task<Order> BuyProductAsync(string memberRowKey, string productRowKey);
        Task<Order> ReturnProductAsync(string loanRowKey);
        Task<List<Order>> GetCustomerBoughtAsync();

        // =====================
        // Activity tracking
        // =====================
        Task SendActivityMessageAsync(string message);
        Task<List<string>> GetRecentActivitiesAsync(int maxMessages = 10);

        // =====================
        // Document management
        // =====================
        Task UploadDocumentsAsync(IFormFile policyFile);
        Task<List<string>> GetDocumentsAsync();
        Task<Stream> DownloadDocumentsAsync(string fileName);

        // =====================
        // Dashboard
        // =====================
        Task<object> GetDashboardStatsAsync();
    }
}
