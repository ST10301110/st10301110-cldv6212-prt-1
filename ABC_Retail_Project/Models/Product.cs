using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Azure;
using Azure.Data.Tables;

namespace ABC_Retail_Project.Models
{
    public class Product : ITableEntity
    {
        [Required(ErrorMessage = "Product name is required")]
        public string Name { get; set; }

        public string Description { get; set; }

        // Store price as string in Azure, convert to decimal in code
        public string PriceString
        {
            get => Price.ToString("F2");
            set
            {
                if (decimal.TryParse(value, out var priceValue))
                {
                    Price = priceValue;
                }
                else
                {
                    Price = 0;
                }
            }
        }

        // This property won't be stored in Azure Table directly
        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative")]
        public int StockQuantity { get; set; }

        public string ImageUrl { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}