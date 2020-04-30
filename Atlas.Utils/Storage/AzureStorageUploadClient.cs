using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Atlas.Utils.Core.ApplicationInsights;
using Atlas.Utils.Storage.ApplicationInsights;

namespace Atlas.Utils.Storage
{
    public interface IStorageClient
    {
        Task Upload(string container, string filename, string messageBody);
        Task<string> Get(string container, string file);
    }

    public class AzureStorageBlobClient : IStorageClient
    {
        private const string UploadLogLabel = "Upload";
        private const string GetLogLabel = "Download";

        private readonly CloudBlobClient blobClient;
        private readonly ILogger logger;

        public AzureStorageBlobClient(string azureStorageConnectionString, ILogger logger)
        {
            this.logger = logger;

            var storageAccount = CloudStorageAccount.Parse(azureStorageConnectionString);
            blobClient = storageAccount.CreateCloudBlobClient();
        }

        public async Task Upload(string container, string filename, string messageBody)
        {
            var azureStorageEventModel = new AzureStorageEventModel(filename, container);
            azureStorageEventModel.StartAzureStorageCommunication();

            var containerRef = await GetBlobContainer(container);
            var blockBlob = containerRef.GetBlockBlobReference(filename);
            blockBlob.Properties.ContentType = "text/plain";
            await blockBlob.UploadTextAsync(messageBody);

            azureStorageEventModel.EndAzureStorageCommunication(UploadLogLabel);
            logger.SendEvent(azureStorageEventModel);
        }
        
        public async Task<string> Get(string container, string file)
        {
            var azureStorageEventModel = new AzureStorageEventModel(file, container);
            azureStorageEventModel.StartAzureStorageCommunication();

            var containerRef = await GetBlobContainer(container);
            var blockBlob = containerRef.GetBlockBlobReference(file);
            var download = await blockBlob.DownloadTextAsync();

            azureStorageEventModel.EndAzureStorageCommunication(GetLogLabel);
            logger.SendEvent(azureStorageEventModel);

            return download;
        }

        private async Task<CloudBlobContainer> GetBlobContainer(string containerName)
        {
            var container = blobClient.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();
            return container;
        }
    }
}
