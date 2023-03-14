using System.Threading.Tasks;
using Azure.Storage.Blobs;

namespace Atlas.Common.AzureStorage.Blob
{
    public abstract class AzureStorageBlobClient
    {
        private readonly BlobServiceClient blobClient;

        protected AzureStorageBlobClient(string azureStorageConnectionString)
        {
            blobClient = new BlobServiceClient(azureStorageConnectionString);
        }

        /// <summary>
        ///  Gets blob container reference without any check to see if it exists or not.
        /// </summary>
        protected BlobContainerClient GetBlobContainer(string containerName)
        {
            return blobClient.GetBlobContainerClient(containerName);
        }

        /// <summary>
        /// Will first create the container if it doesn't exist, and then returns the container.
        /// </summary>
        protected async Task<BlobContainerClient> CreateAndGetBlobContainer(string containerName)
        {
            var container = blobClient.GetBlobContainerClient(containerName);

            // avoiding the use of the `CreateIfNotExists` method as it seems to send a CreateContainer API call even when the container already exists.

            if (await container.ExistsAsync())
            {
                return container;
            }

            await container.CreateAsync();
            return container;
        }
    }
}