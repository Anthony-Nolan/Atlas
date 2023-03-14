using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.ApplicationInsights;
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

        public async Task Upload(string container, string filename, string fileContents)
        {
            var azureStorageEventModel = new AzureStorageEventModel(filename, container);
            azureStorageEventModel.StartAzureStorageCommunication();

            var containerClient = await CreateAndGetBlobContainer(container);
            var blobClient = containerClient.GetBlobClient(filename);
            await blobClient.UploadAsync(BinaryData.FromString(fileContents), overwrite: true);

            azureStorageEventModel.EndAzureStorageCommunication(UploadLogLabel);
            Logger.SendEvent(azureStorageEventModel);
        }
    }
}