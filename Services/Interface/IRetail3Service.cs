using Retail3.Models;

namespace Retail3.Services.Interface
{
    public interface IRetail3Service
    {
        // =====================
        // Customer operations
        // =====================

        // Adds a new member to the library
        Task<Customer> AddCustomerAsync(Customer customer);

        // Retrieves all members from the library
        Task<List<Customer>> GetAllCustomersAsync();

        // Retrieves a single member by their unique RowKey
        Task<Customer?> GetCustomerAsync(string rowKey);


        // =====================
        // Product operations
        // =====================

        // Adds a new book, optionally with a cover image
        Task<Product> AddProductAsync(Product product, IFormFile? productImage);

        // Retrieves all books in the library
        Task<List<Product>> GetAllProductsAsync();

        // Retrieves a single book by its RowKey
        Task<Product?> GetProductAsync(string rowKey);

        // Updates an existing book, optionally updating its cover image
        Task UpdateProductAsync(Product product, IFormFile? productImage);

        // Deletes a book by its RowKey
        Task DeleteProductAsync(string rowKey);


        // =====================
        // Order operations
        // =====================

        // Borrows a book for a member; returns a BookLoan object
        Task<Order> BuyProductAsync(string memberRowKey, string bookRowKey);

        // Returns a borrowed book; updates condition and notes
        Task<Order> ReturnProductAsync(string loanRowKey);

        // Retrieves all currently active loans (books that are borrowed but not yet returned)
        Task<List<Order>> GetCustomerBoughtAsync();


        // =====================
        // Activity tracking
        // =====================

        // Sends a message to track activities (e.g., "Book borrowed by John")
        Task SendActivityMessageAsync(string message);

        // Retrieves the most recent activity messages
        Task<List<string>> GetRecentActivitiesAsync(int maxMessages = 10);


        // =====================
        // Document management
        // =====================

        // Uploads a library policy file (PDF, DOCX, etc.)
        Task UploadDocumentsAsync(IFormFile policyFile);

        // Gets the list of uploaded library policy file names
        Task<List<string>> GetDocumentsAsync();

        // Downloads a library policy file by its name
        Task<Stream> DownloadDocumentsAsync(string fileName);


        // =====================
        // Dashboard
        // =====================

        // Retrieves summary statistics for the dashboard (could include total members, books, loans, etc.)
        Task<object> GetDashboardStatsAsync();
    }
}
