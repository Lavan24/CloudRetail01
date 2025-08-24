using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace Retail3.Models
{
    public class Order : ITableEntity
    {
        //---------------------DATA FOR TABLE STORAGE---------------------
        public string PartitionKey { get; set; } = "orders";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Loan relationships
        public string CustomerRowKey { get; set; } = string.Empty;
        public string ProductRowKey { get; set; } = string.Empty;

        //Order Information
        [Required]
        public DateTime OrderDate { get; set; }
        [Required]
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }

        // Denormalized data for display
        public string FullName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
    }
}
