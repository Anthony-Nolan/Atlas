using System;
using Atlas.Common.ApplicationInsights;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using MoreLinq;
using System.IO;
using Atlas.Common.Utils.Extensions;

namespace Atlas.Common.AzureStorage.Blob
{
    public class BlobUploader : AzureStorageBlobClient
    {
        private readonly JsonSerializer serializer;

        public BlobUploader(string azureStorageConnectionString, ILogger logger) : base(azureStorageConnectionString, logger, "Upload")
        {
            serializer = new JsonSerializer();
        }

        public async Task Upload<T>(string container, string filename, T fileContents)
        {
            var azureStorageEventModel = StartAzureStorageCommunication(filename, container);

            var containerClient = await CreateAndGetBlobContainer(container);
            await UploadBlob(containerClient, filename, fileContents);

            EndAzureStorageCommunication(azureStorageEventModel);
        }

        public async Task ChunkAndUpload<T>(IEnumerable<T> list, int batchSize, string blobContainer, string blobFolder)
        {
            var azureStorageEventModel = StartAzureStorageCommunication(blobFolder, blobContainer);

            var containerClient = await CreateAndGetBlobContainer(blobContainer);

            var batchNumber = 0;
            foreach (var batch in list.Batch(batchSize))
            {
                await UploadBlob(containerClient, $"{blobFolder}/{++batchNumber}.json", batch);
            }

            EndAzureStorageCommunication(azureStorageEventModel);
        }

        public async Task UploadMultiple<T>(string blobContainer, Dictionary<string, T> fileContentsWithNames)
        {
            var azureStorageEventModel = StartAzureStorageCommunication(blobContainer, blobContainer);

            var containerClient = await CreateAndGetBlobContainer(blobContainer);

            foreach (var file in fileContentsWithNames)
            {
                await UploadBlob(containerClient, file.Key, file.Value);
            }

            EndAzureStorageCommunication(azureStorageEventModel);
        }

        private async Task UploadBlob<T>(BlobContainerClient containerClient, string filename, T fileContents)
        {
            using var memoryStream = new MemoryStream();
            serializer.SerializeToStream(fileContents, memoryStream);

            memoryStream.Seek(0, SeekOrigin.Begin); // Stream position will be on the end of the stream. Need to rewind it to the beginning

            var blobClient = containerClient.GetBlobClient(filename);
            var uploadOptions = new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = "text/plain" } };

            await blobClient.UploadAsync(memoryStream, uploadOptions);
        }
    }
}