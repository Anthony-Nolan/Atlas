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

            var containerRef = await GetBlobContainer(container);
            var blockBlob = containerRef.GetBlockBlobReference(filename);

            var data = JsonConvert.DeserializeObject<T>(await blockBlob.DownloadTextAsync());
            
            azureStorageEventModel.EndAzureStorageCommunication(UploadLogLabel);
            logger.SendEvent(azureStorageEventModel);

            return data;
        }
    }
}