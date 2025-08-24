using System.ComponentModel.DataAnnotations;
using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Retail3.Models
{
    public class Customer : ITableEntity
    {
        //---------------------DATA FOR TABLE STORAGE---------------------
        [ValidateNever]
        public string PartitionKey { get; set; }

        [ValidateNever]
        public string RowKey { get; set; }

        [ValidateNever]
        public DateTimeOffset? Timestamp { get; set; }

        [ValidateNever]
        public ETag ETag { get; set; }

        //---------------------VARIABLES FOR CUSTOMER---------------------
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone format")]
        public string Phone { get; set; }

        public string FullName => $"{FirstName} {LastName}";

        public string? IdImagePath { get; set; }
    }
}
