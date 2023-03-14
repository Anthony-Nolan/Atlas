using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.ApplicationInsights;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.Common.AzureStorage.Blob
{
    public class BlobUploader : AzureStorageBlobClient
    {
        private const string UploadLogLabel = "Upload";
        protected readonly ILogger Logger;

        public BlobUploader(string azureStorageConnectionString, ILogger logger) : base(azureStorageConnectionString)
        {
            Logger = logger;
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
            Logger.SendEvent(azureStorageEventModel);
        }

        public async Task BatchUpload<T>(IEnumerable<T> list, int batchSize, string blobContainer, string blobFolder)
        {
            var batchNumber = 0;
            var listSize = list.Count();
            while (listSize > batchNumber * batchSize)
            {
                var serializedBatch = JsonConvert.SerializeObject(GetBatch(list, batchNumber++, batchSize));
                await Upload(blobContainer, $"{blobFolder}/{batchNumber}.json", serializedBatch);
            }
        }

        private IEnumerable<T> GetBatch<T>(IEnumerable<T> list, int batchNumber, int batchSize) =>
            list.Skip(batchNumber * batchSize).Take(batchSize);
    }
}