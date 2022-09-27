using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;

namespace Atlas.Common.AzureStorage.Blob
{
    public interface IBlobStreamer
    {
        Task<Stream> GetBlobContents(string containerName, string blobName);
    }

    public class BlobStreamer : AzureStorageBlobClient, IBlobStreamer
    {
        public BlobStreamer(string azureStorageConnectionString) : base(azureStorageConnectionString)
        {
        }

        public async Task<Stream> GetBlobContents(string containerName, string blobName)
        {
            return await GetContentStream(containerName, blobName);
        }

        private async Task<Stream> GetContentStream(string containerName, string fileName)
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