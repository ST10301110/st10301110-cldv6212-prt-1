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

                var queueMessage = new OrderQueueMessage
                {
                    PartitionKey = order.PartitionKey,
                    RowKey = order.RowKey,
                    CustomerId = order.CustomerId,
                    ProductId = order.ProductId,
                    Quantity = order.Quantity,
                    TotalAmount = (double)order.TotalAmount,
                    OrderDate = order.OrderDate,
                    Status = order.Status
                };

                var messageJson = System.Text.Json.JsonSerializer.Serialize(queueMessage);
                await _queueClient.SendMessageAsync(messageJson);

                Console.WriteLine("Order successfully added to queue! Function will process it.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ORDER QUEUE ERROR: {ex.Message}");
                throw;
            }
        }

        public class OrderQueueMessage
        {
            public string PartitionKey { get; set; }
            public string RowKey { get; set; }
            public string CustomerId { get; set; }
            public string ProductId { get; set; }
            public int Quantity { get; set; }
            public double TotalAmount { get; set; }
            public DateTime OrderDate { get; set; }
            public string Status { get; set; }
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

        public async Task UpdateOrderStatusAsync(string orderId, string newStatus)
        {
            try
            {
                var order = await GetOrderAsync("Order", orderId);
                if (order != null && order.CanUpdateStatus(newStatus))
                {
                    order.Status = newStatus;
                    await UpdateOrderAsync(order);
                }
                else
                {
                    throw new InvalidOperationException($"Invalid status transition from {order?.Status} to {newStatus}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating order status: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Order>> GetOrdersByStatusAsync(string status)
        {
            var orders = await GetOrdersWithDetailsAsync();
            return orders.Where(o => o.Status == status).ToList();
        }
    }
}