using Azure;
using Azure.Storage.Files.Shares;

namespace ABC_Retail_Project.Models
{
    public class ContractService
    {
        private readonly ShareClient _shareClient;

        public ContractService(string connectionString)
        {
            _shareClient = new ShareClient(connectionString, "contracts");
            _shareClient.CreateIfNotExists();
        }

        public async Task UploadContractAsync(IFormFile contractFile, string customerId)
        {
            if (contractFile == null || contractFile.Length == 0)
            {
                throw new ArgumentException("No file uploaded or file is empty");
            }

            var directoryClient = _shareClient.GetDirectoryClient(customerId);
            await directoryClient.CreateIfNotExistsAsync();

            var fileClient = directoryClient.GetFileClient(contractFile.FileName);

            using var stream = contractFile.OpenReadStream();
            await fileClient.CreateAsync(stream.Length);
            await fileClient.UploadRangeAsync(
                new HttpRange(0, stream.Length),
                stream);
        }
    }
}