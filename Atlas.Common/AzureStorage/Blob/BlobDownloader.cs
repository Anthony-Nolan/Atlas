using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.ApplicationInsights;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Atlas.Common.AzureStorage.Blob
{
    public interface IBlobDownloader
    {
        Task<T> Download<T>(string container, string filename);
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
            var blobClient = containerClient.GetBlobClient(filename);
            var downloadedBlob = await blobClient.DownloadContentAsync();

            if (downloadedBlob is not { HasValue: true })
            {
                throw new BlobNotFoundException(container, filename);
            }

            var data = JsonConvert.DeserializeObject<T>(downloadedBlob.Value.Content.ToString());
            
            azureStorageEventModel.EndAzureStorageCommunication(UploadLogLabel);
            logger.SendEvent(azureStorageEventModel);

            return data;
        }
    }
}