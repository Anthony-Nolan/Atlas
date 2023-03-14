using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.ApplicationInsights;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Storage.Blob;

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

            var containerRef = await GetBlobContainer(container);
            var blockBlob = containerRef.GetBlockBlobReference(filename);

            var data = JsonConvert.DeserializeObject<T>(await blockBlob.DownloadTextAsync());
            
            azureStorageEventModel.EndAzureStorageCommunication(UploadLogLabel);
            logger.SendEvent(azureStorageEventModel);

            return data;
        }

        public async Task<IEnumerable<T>> BatchDownload<T>(string container, string folderName)
        {
            var data = new List<T>();

            var azureStorageEventModel = new AzureStorageEventModel(folderName, container);
            azureStorageEventModel.StartAzureStorageCommunication();

            var containerRef = await GetBlobContainer(container);

            var blobs = containerRef.ListBlobs(prefix: $"{folderName}/", useFlatBlobListing: true);

            if (blobs?.Any() == true)
            {
                var fileNames = blobs.OfType<CloudBlockBlob>().Select(b => b.Name);
                foreach (var fileName in fileNames)
                {
                    var blockBlob = containerRef.GetBlockBlobReference(fileName);
                    data.AddRange(JsonConvert.DeserializeObject<IEnumerable<T>>(await blockBlob.DownloadTextAsync()));
                }
            }

            azureStorageEventModel.EndAzureStorageCommunication(UploadLogLabel);
            logger.SendEvent(azureStorageEventModel);

            return data;
        }
    }
}