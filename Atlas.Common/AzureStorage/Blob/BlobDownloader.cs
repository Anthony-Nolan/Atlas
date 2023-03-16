using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.ApplicationInsights;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using Azure.Storage.Blobs;

namespace Atlas.Common.AzureStorage.Blob
{
    public interface IBlobDownloader
    {
        Task<T> Download<T>(string container, string filename);
        Task<IEnumerable<T>> BatchDownload<T>(string container, string folderName);
    }
    
    public class BlobDownloader : AzureStorageBlobClient, IBlobDownloader
    {
        private const string UploadLogLabel = "Download";
        private readonly ILogger logger;

        public BlobDownloader(string azureStorageConnectionString, ILogger logger) : base(azureStorageConnectionString)
        {
            this.logger = logger;
        }

        public async Task<T> Download<T>(string container, string filename)
        {
            var azureStorageEventModel = new AzureStorageEventModel(filename, container);
            azureStorageEventModel.StartAzureStorageCommunication();

            var containerClient = GetBlobContainer(container);
            var data = await GetBlobData<T>(containerClient, filename);

            azureStorageEventModel.EndAzureStorageCommunication(UploadLogLabel);
            logger.SendEvent(azureStorageEventModel);

            return data;
        }

        public async Task<IEnumerable<T>> BatchDownload<T>(string container, string folderName)
        {
            var data = new List<T>();

            var azureStorageEventModel = new AzureStorageEventModel(folderName, container);
            azureStorageEventModel.StartAzureStorageCommunication();

            var containerClient = GetBlobContainer(container);
            var blobs =  containerClient.GetBlobsAsync(prefix: $"{folderName}/");

            await foreach (var blob in blobs)
            {
                data.AddRange(await GetBlobData<IEnumerable<T>>(containerClient, blob.Name));
            }

            azureStorageEventModel.EndAzureStorageCommunication(UploadLogLabel);
            logger.SendEvent(azureStorageEventModel);

            return data;
        }

        private async Task<T> GetBlobData<T>(BlobContainerClient containerClient, string filename)
        {
            var blobClient = containerClient.GetBlobClient(filename);
            var downloadedBlob = await blobClient.DownloadContentAsync();

            if (downloadedBlob is not { HasValue: true })
            {
                throw new BlobNotFoundException(containerClient.Name, filename);
            }

            return JsonConvert.DeserializeObject<T>(downloadedBlob.Value.Content.ToString());
        }
    }
}