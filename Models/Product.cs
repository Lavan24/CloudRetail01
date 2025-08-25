using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace Retail3.Models
{
    public class Product : ITableEntity
    {
        //---------------------DATA FOR TABLE STORAGE---------------------
        public string PartitionKey { get; set; } = "products";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        //---------------------VARIABLES FOR PRODUCT---------------------
        [Required, StringLength(100)]
        public string ProductName { get; set; }

        [StringLength(255)]
        public string Description { get; set; }

        [Required]
        public double Price { get; set; }

        [Required]
        public int StockQuantity { get; set; }

        public string Category { get; set; }

        public string productImageURL { get; set; } = string.Empty;

    }
}
