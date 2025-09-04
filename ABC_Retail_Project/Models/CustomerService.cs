using Azure;
using Azure.Data.Tables;

namespace ABC_Retail_Project.Models
{
    public class CustomerService
    {
        private readonly TableClient _tableClient;

        public CustomerService(TableServiceClient tableServiceClient)
        {
            _tableClient = tableServiceClient.GetTableClient("Customers");
            _tableClient.CreateIfNotExists();
        }

        public async Task AddCustomerAsync(Customer customer)
        {
            customer.PartitionKey = "Customer";
            customer.RowKey ??= Guid.NewGuid().ToString();
            await _tableClient.AddEntityAsync(customer);
        }

        public async Task<List<Customer>> GetCustomersAsync()
        {
            var customers = new List<Customer>();
            await foreach (var customer in _tableClient.QueryAsync<Customer>())
            {
                customers.Add(customer);
            }
            return customers;
        }

        public async Task<Customer> GetCustomerAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<Customer>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task UpdateCustomerAsync(Customer customer)
        {
            await _tableClient.UpdateEntityAsync(customer, ETag.All, TableUpdateMode.Replace);
        }

        public async Task DeleteCustomerAsync(string partitionKey, string rowKey)
        {
            await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
        }
    }
}