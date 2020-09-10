using System.IO;
using System.Threading.Tasks;

namespace Atlas.Common.AzureStorage.Blob
{
    public abstract class BlobStreamer : AzureStorageBlobClient
    {
        protected BlobStreamer(string azureStorageConnectionString) : base(azureStorageConnectionString)
        {
        }

        protected async Task<Stream> GetContentStream(string containerName, string fileName)
        {
            var blob = await GetCloudBlob(containerName, fileName);
            return await blob.OpenReadAsync();
        }

        private async Task<CloudBlob> GetCloudBlob(string containerName, string fileName)
        {
            var container = await GetBlobContainer(containerName);
            return container.GetBlobReference(fileName);
        }
    }
}