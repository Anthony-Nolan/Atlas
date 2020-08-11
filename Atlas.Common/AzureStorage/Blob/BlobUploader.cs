using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.ApplicationInsights;
using System.Threading.Tasks;

namespace Atlas.Common.AzureStorage.Blob
{
    public abstract class BlobUploader : AzureStorageBlobClient
    {
        private const string UploadLogLabel = "Upload";
        private readonly ILogger searchLogger;

        protected BlobUploader(string azureStorageConnectionString, ILogger searchLogger) : base(azureStorageConnectionString)
        {
            this.searchLogger = searchLogger;
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
            searchLogger.SendEvent(azureStorageEventModel);
        }
    }
}