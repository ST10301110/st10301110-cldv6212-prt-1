using Azure;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using System.Globalization;

namespace ABC_Retail_Project.Models
{
    public class OrderService
    {
        private readonly TableClient _tableClient;
        private readonly QueueClient _queueClient;
        private readonly CustomerService _customerService;
        private readonly ProductService _productService;

        public OrderService(TableServiceClient tableServiceClient, QueueServiceClient queueServiceClient, CustomerService customerService, ProductService productService)
        {
            _tableClient = tableServiceClient.GetTableClient("Orders");
            _tableClient.CreateIfNotExists();

            _queueClient = queueServiceClient.GetQueueClient("orders-queue");
            _queueClient.CreateIfNotExists();

            _customerService = customerService;
            _productService = productService;
        }

        public async Task AddOrderToQueueAsync(Order order)
        {
            Console.WriteLine("=== ADDING ORDER TO QUEUE ===");
            Console.WriteLine($"Order ID: {order.RowKey}");
            Console.WriteLine($"Customer: {order.CustomerId}");
            Console.WriteLine($"Product: {order.ProductId}");
            Console.WriteLine($"Quantity: {order.Quantity}");
            Console.WriteLine($"Total Amount: {order.TotalAmount}");
            Console.WriteLine($"Order Date: {order.OrderDate}");
            Console.WriteLine($"Status: {order.Status}");

            try
            {
                // Add to table storage with ALL fields
                var entity = new TableEntity(order.PartitionKey, order.RowKey)
                {
                    ["CustomerId"] = order.CustomerId,
                    ["ProductId"] = order.ProductId,
                    ["Quantity"] = order.Quantity,
                    ["TotalAmount"] = (double)order.TotalAmount, // Convert to double for storage
                    ["OrderDate"] = order.OrderDate,
                    ["Status"] = order.Status
                };

                await _tableClient.AddEntityAsync(entity);
                Console.WriteLine("Order successfully added to table with all fields!");

                // Add to queue (if you have queue functionality)
                // await _queueClient.SendMessageAsync(order.RowKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ORDER QUEUE ERROR: {ex.Message}");
                throw;
            }
        }

        private decimal GetTotalAmountFromEntity(TableEntity entity)
        {
            try
            {
                // First try to get as double (new format)
                if (entity.TryGetValue("TotalAmount", out var value) && value is double doubleValue)
                {
                    return Convert.ToDecimal(doubleValue);
                }

                // Then try to get as string (old format)
                if (entity.TryGetValue("TotalAmount", out value) && value is string stringValue)
                {
                    return decimal.Parse(stringValue, CultureInfo.InvariantCulture);
                }

                // Fallback to default
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private int GetQuantityFromEntity(TableEntity entity)
        {
            try
            {
                return entity.GetInt32("Quantity") ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private string GetProductIdFromEntity(TableEntity entity)
        {
            try
            {
                return entity.GetString("ProductId") ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private DateTime GetOrderDateFromEntity(TableEntity entity)
        {
            try
            {
                return entity.GetDateTime("OrderDate") ?? DateTime.MinValue;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        public async Task<List<Order>> GetOrdersAsync()
        {
            var orders = new List<Order>();
            await foreach (var entity in _tableClient.QueryAsync<TableEntity>())
            {
                var order = new Order
                {
                    PartitionKey = entity.PartitionKey,
                    RowKey = entity.RowKey,
                    CustomerId = entity.GetString("CustomerId"),
                    ProductId = GetProductIdFromEntity(entity),
                    Quantity = GetQuantityFromEntity(entity),
                    TotalAmount = GetTotalAmountFromEntity(entity),
                    OrderDate = GetOrderDateFromEntity(entity),
                    Status = entity.GetString("Status"),
                    Timestamp = entity.Timestamp,
                    ETag = entity.ETag
                };
                orders.Add(order);
            }
            return orders;
        }

        public async Task<List<Order>> GetOrdersWithDetailsAsync()
        {
            var orders = await GetOrdersAsync();
            var customers = await _customerService.GetCustomersAsync();
            var products = await _productService.GetProductsAsync();

            foreach (var order in orders)
            {
                order.Customer = customers.FirstOrDefault(c => c.RowKey == order.CustomerId);
                order.Product = products.FirstOrDefault(p => p.RowKey == order.ProductId);
            }

            return orders;
        }

        public async Task<Order> GetOrderAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<TableEntity>(partitionKey, rowKey);
                var entity = response.Value;

                return new Order
                {
                    PartitionKey = entity.PartitionKey,
                    RowKey = entity.RowKey,
                    CustomerId = entity.GetString("CustomerId"),
                    ProductId = GetProductIdFromEntity(entity),
                    Quantity = GetQuantityFromEntity(entity),
                    TotalAmount = GetTotalAmountFromEntity(entity),
                    OrderDate = GetOrderDateFromEntity(entity),
                    Status = entity.GetString("Status"),
                    Timestamp = entity.Timestamp,
                    ETag = entity.ETag
                };
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task UpdateOrderAsync(Order order)
        {
            var entity = new TableEntity(order.PartitionKey, order.RowKey)
            {
                ["CustomerId"] = order.CustomerId,
                ["ProductId"] = order.ProductId,
                ["Quantity"] = order.Quantity,
                ["TotalAmount"] = (double)order.TotalAmount, // Convert to double for storage
                ["OrderDate"] = order.OrderDate,
                ["Status"] = order.Status
            };

            await _tableClient.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Replace);
        }

        public async Task DeleteOrderAsync(string partitionKey, string rowKey)
        {
            await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
        }
    }
}