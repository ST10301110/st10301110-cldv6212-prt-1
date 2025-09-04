using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;

namespace ABC_Retail_Project.Models
{
    public class ProductService
    {
        private readonly TableClient _tableClient;
        private readonly BlobContainerClient _containerClient;

        public ProductService(TableServiceClient tableServiceClient, BlobServiceClient blobServiceClient)
        {
            _tableClient = tableServiceClient.GetTableClient("Products");
            _tableClient.CreateIfNotExists();

            _containerClient = blobServiceClient.GetBlobContainerClient("product-images");
            _containerClient.CreateIfNotExists();
        }

        public async Task AddProductAsync(Product product)
        {
            Console.WriteLine("=== SAVING PRODUCT TO DATABASE ===");
            Console.WriteLine($"Name: {product.Name}");
            Console.WriteLine($"Price: {product.Price}");
            Console.WriteLine($"Stock: {product.StockQuantity}");
            Console.WriteLine($"ImageUrl: {product.ImageUrl}");
            Console.WriteLine($"PartitionKey: {product.PartitionKey}");
            Console.WriteLine($"RowKey: {product.RowKey}");

            try
            {
                // Create a TableEntity that Azure can handle better
                var entity = new TableEntity(product.PartitionKey, product.RowKey)
                {
                    ["Name"] = product.Name,
                    ["Description"] = product.Description,
                    ["PriceString"] = product.Price.ToString("F2"), // Store as string
                    ["StockQuantity"] = product.StockQuantity,
                    ["ImageUrl"] = product.ImageUrl
                };

                await _tableClient.AddEntityAsync(entity);
                Console.WriteLine("Product successfully saved to Azure Table!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DATABASE SAVE ERROR: {ex.Message}");
                Console.WriteLine($"Error Type: {ex.GetType().Name}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                // Check for specific Azure Table errors
                if (ex is RequestFailedException azureEx)
                {
                    Console.WriteLine($"Azure Status Code: {azureEx.Status}");
                    Console.WriteLine($"Azure Error Code: {azureEx.ErrorCode}");
                }

                throw;
            }
        }

        public async Task<List<Product>> GetProductsAsync()
        {
            var products = new List<Product>();

            await foreach (var entity in _tableClient.QueryAsync<TableEntity>())
            {
                var product = new Product
                {
                    PartitionKey = entity.PartitionKey,
                    RowKey = entity.RowKey,
                    Name = entity.GetString("Name"),
                    Description = entity.GetString("Description"),
                    // Get price as string and convert to decimal
                    PriceString = entity.GetString("PriceString"),
                    StockQuantity = entity.GetInt32("StockQuantity") ?? 0,
                    ImageUrl = entity.GetString("ImageUrl"),
                    Timestamp = entity.Timestamp,
                    ETag = entity.ETag
                };

                Console.WriteLine($"DEBUG - Product: {product.Name}, " +
                                 $"Price: {product.Price}, " +
                                 $"PriceString: {product.PriceString}");

                products.Add(product);
            }

            return products;
        }

        public async Task<Product> GetProductAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<TableEntity>(partitionKey, rowKey);
                var entity = response.Value;

                var product = new Product
                {
                    PartitionKey = entity.PartitionKey,
                    RowKey = entity.RowKey,
                    Name = entity.GetString("Name"),
                    Description = entity.GetString("Description"),
                    // Get price as string and convert to decimal
                    PriceString = entity.GetString("PriceString"),
                    StockQuantity = entity.GetInt32("StockQuantity") ?? 0,
                    ImageUrl = entity.GetString("ImageUrl"),
                    Timestamp = entity.Timestamp,
                    ETag = entity.ETag
                };

                return product;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task UpdateProductAsync(Product product)
        {
            // Create a TableEntity for update
            var entity = new TableEntity(product.PartitionKey, product.RowKey)
            {
                ["Name"] = product.Name,
                ["Description"] = product.Description,
                ["PriceString"] = product.Price.ToString("F2"), // Store as string
                ["StockQuantity"] = product.StockQuantity,
                ["ImageUrl"] = product.ImageUrl,
                ETag = product.ETag
            };

            await _tableClient.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Replace);
        }

        public async Task DeleteProductAsync(string partitionKey, string rowKey)
        {
            await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        public async Task<string> UploadImageAsync(IFormFile imageFile, string productId)
        {
            Console.WriteLine($"=== UPLOAD IMAGE STARTED ===");
            Console.WriteLine($"File: {imageFile.FileName}, Size: {imageFile.Length}, Type: {imageFile.ContentType}");

            if (imageFile == null || imageFile.Length == 0)
            {
                Console.WriteLine("No image file provided");
                return null;
            }

            try
            {
                // Generate unique blob name
                var blobName = $"{productId}-{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
                Console.WriteLine($"Blob name: {blobName}");

                var blobClient = _containerClient.GetBlobClient(blobName);
                Console.WriteLine($"Blob client created for: {blobClient.Uri}");

                // Upload the file
                using var stream = imageFile.OpenReadStream();
                var response = await blobClient.UploadAsync(stream, overwrite: true);

                Console.WriteLine($"Upload successful! Status: {response.GetRawResponse().Status}");

                // Return the URL
                var imageUrl = blobClient.Uri.ToString();
                Console.WriteLine($"Image URL: {imageUrl}");

                return imageUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UPLOAD ERROR: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return null;
            }
        }
    }
}