using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.ApplicationInsights;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Atlas.Common.AzureStorage.Blob
{
    public abstract class AzureStorageBlobClient
    {
        private const string UploadLogLabel = "Upload";

        private readonly CloudBlobClient blobClient;
        private readonly ILogger logger;

        protected AzureStorageBlobClient(string azureStorageConnectionString, ILogger logger)
        {
            this.logger = logger;

            var storageAccount = CloudStorageAccount.Parse(azureStorageConnectionString);
            blobClient = storageAccount.CreateCloudBlobClient();
        }

        protected async Task Upload(string container, string filename, string messageBody)
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

        private async Task<CloudBlobContainer> GetBlobContainer(string containerName)
        {
            var container = blobClient.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();
            return container;
        }
    }
}