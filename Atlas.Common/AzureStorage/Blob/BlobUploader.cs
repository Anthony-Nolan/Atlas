using System;
using Atlas.Common.ApplicationInsights;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using MoreLinq;

namespace Atlas.Common.AzureStorage.Blob
{
    public class BlobUploader : AzureStorageBlobClient
    {
        public BlobUploader(string azureStorageConnectionString, ILogger logger) : base(azureStorageConnectionString, logger, "Upload")
        {
        }

        public async Task Upload(string container, string filename, string fileContents)
        {
            var azureStorageEventModel = StartAzureStorageCommunication(filename, container);

            var containerClient = await CreateAndGetBlobContainer(container);
            await UploadBlob(containerClient, filename, fileContents);

            EndAzureStorageCommunication(azureStorageEventModel);
        }

        public async Task BatchUpload<T>(IEnumerable<T> list, int batchSize, string blobContainer, string blobFolder)
        {
            var azureStorageEventModel = StartAzureStorageCommunication(blobFolder, blobContainer);

            var containerClient = await CreateAndGetBlobContainer(blobContainer);

            var batchNumber = 0;
            foreach (var batch in list.Batch(batchSize))
            {
                var serializedBatch = JsonConvert.SerializeObject(batch);
                await UploadBlob(containerClient, $"{blobFolder}/{++batchNumber}.json", serializedBatch);
            }

            EndAzureStorageCommunication(azureStorageEventModel);
        }

        private async Task UploadBlob(BlobContainerClient containerClient, string filename, string fileContents)
        {
            var blobClient = containerClient.GetBlobClient(filename);
            var uploadOptions = new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = "text/plain" } };
            await blobClient.UploadAsync(BinaryData.FromString(fileContents), uploadOptions);
        }
    }
}