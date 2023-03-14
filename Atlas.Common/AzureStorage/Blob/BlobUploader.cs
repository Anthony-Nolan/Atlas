using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.ApplicationInsights;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;

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
            var uploadOptions = new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = "text/plain" } };
            await blobClient.UploadAsync(BinaryData.FromString(fileContents), uploadOptions);

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