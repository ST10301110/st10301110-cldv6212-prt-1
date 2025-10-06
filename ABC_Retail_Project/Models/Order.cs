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

        [Required(ErrorMessage = "Status is required")]
        [Display(Name = "Order Status")]
        public string Status { get; set; } = "Pending";

        public bool CanUpdateStatus(string newStatus)
        {
            var validTransitions = new Dictionary<string, List<string>>
            {
                { "Pending", new List<string> { "Processing", "Cancelled" } },
                { "Processing", new List<string> { "Shipped", "Cancelled" } },
                { "Shipped", new List<string> { "Delivered", "Returned" } },
                { "Delivered", new List<string> { "Completed", "Returned" } },
                { "Cancelled", new List<string>() },
                { "Completed", new List<string>() },
                { "Returned", new List<string> { "Refunded" } },
                { "Refunded", new List<string>() }
            };

            return validTransitions.ContainsKey(Status) &&
                   validTransitions[Status].Contains(newStatus);
        }

        public static List<string> GetAllStatuses()
        {
            return new List<string>
            {
                "Pending",
                "Processing",
                "Shipped",
                "Delivered",
                "Completed",
                "Cancelled",
                "Returned",
                "Refunded"
            };
        }

        public string GetStatusBadgeClass()
        {
            return Status switch
            {
                "Pending" => "badge badge-warning",
                "Processing" => "badge badge-info",
                "Shipped" => "badge badge-primary",
                "Delivered" => "badge badge-success",
                "Completed" => "badge badge-success",
                "Cancelled" => "badge badge-danger",
                "Returned" => "badge badge-secondary",
                "Refunded" => "badge badge-dark",
                _ => "badge badge-light"
            };
        }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        [IgnoreDataMember]
        public Customer Customer { get; set; }

        [IgnoreDataMember]
        public Product Product { get; set; }

        // Display properties - marked with IgnoreDataMember to prevent validation
        [IgnoreDataMember]
        [Display(Name = "Customer Name")]
        public string CustomerName => Customer?.Name;

        [IgnoreDataMember]
        [Display(Name = "Product Name")]
        public string ProductName => Product?.Name;

        [IgnoreDataMember]
        [Display(Name = "Total Amount (R)")]
        public string FormattedTotalAmount => TotalAmount.ToString("C", new CultureInfo("en-ZA"));

        [IgnoreDataMember]
        [Display(Name = "Order Date")]
        public string FormattedOrderDate => OrderDate.ToString("yyyy-MM-dd");
    }
}