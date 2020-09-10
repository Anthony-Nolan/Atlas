using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace Atlas.Common.AzureStorage.Blob
{
    public abstract class AzureStorageBlobClient
    {
        private readonly CloudBlobClient blobClient;

        protected AzureStorageBlobClient(string azureStorageConnectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(azureStorageConnectionString);
            blobClient = storageAccount.CreateCloudBlobClient();
        }

        protected async Task<CloudBlobContainer> GetBlobContainer(string containerName)
        {
            var container = blobClient.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();
            return container;
        }
    }
}