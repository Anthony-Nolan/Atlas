using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.ApplicationInsights;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using MoreLinq;

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
            await UploadBlob(containerClient, filename, fileContents);

            azureStorageEventModel.EndAzureStorageCommunication(UploadLogLabel);
            Logger.SendEvent(azureStorageEventModel);
        }

        public async Task BatchUpload<T>(IEnumerable<T> list, int batchSize, string blobContainer, string blobFolder)
        {
            var azureStorageEventModel = new AzureStorageEventModel(blobFolder, blobContainer);
            azureStorageEventModel.StartAzureStorageCommunication();

            var containerClient = await CreateAndGetBlobContainer(blobContainer);

            var batchNumber = 0;
            foreach (var batch in list.Batch(batchSize))
            {
                var serializedBatch = JsonConvert.SerializeObject(batch);
                await UploadBlob(containerClient, $"{blobFolder}/{++batchNumber}.json", serializedBatch);
            }

            azureStorageEventModel.EndAzureStorageCommunication(UploadLogLabel);
            Logger.SendEvent(azureStorageEventModel);
        }

        private async Task UploadBlob(BlobContainerClient containerClient, string filename, string fileContents)
        {
            var blobClient = containerClient.GetBlobClient(filename);
            var uploadOptions = new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = "text/plain" } };
            await blobClient.UploadAsync(BinaryData.FromString(fileContents), uploadOptions);
        }
    }
}