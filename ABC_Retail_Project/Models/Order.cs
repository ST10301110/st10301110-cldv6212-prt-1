using System.ComponentModel.DataAnnotations;
using Azure;
using Azure.Data.Tables;
using System.Runtime.Serialization;
using System.Globalization;

namespace ABC_Retail_Project.Models
{
    public class Order : ITableEntity
    {
        [Required(ErrorMessage = "Customer is required")]
        [Display(Name = "Customer")]
        public string CustomerId { get; set; }

        [Required(ErrorMessage = "Product is required")]
        [Display(Name = "Product")]
        public string ProductId { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Total amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Total amount must be greater than 0")]
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Order Date")]
        public DateTime OrderDate { get; set; }

        public string Status { get; set; }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        [IgnoreDataMember]
        public Customer Customer { get; set; }

        [IgnoreDataMember]
        public Product Product { get; set; }

        [Display(Name = "Customer Name")]
        public string CustomerName => Customer?.Name;

        [Display(Name = "Product Name")]
        public string ProductName => Product?.Name;

        [Display(Name = "Total Amount (R)")]
        public string FormattedTotalAmount => TotalAmount.ToString("C", new CultureInfo("en-ZA"));

        [Display(Name = "Order Date")]
        public string FormattedOrderDate => OrderDate.ToString("yyyy-MM-dd");
    }
}