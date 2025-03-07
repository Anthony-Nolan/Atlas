﻿using Atlas.Common.ApplicationInsights;
using System.IO;
using System.Threading.Tasks;

namespace Atlas.Common.AzureStorage.Blob
{
    public interface IBlobStreamer
    {
        Task<Stream> GetBlobContents(string containerName, string blobName);
    }

    public class BlobStreamer : AzureStorageBlobClient, IBlobStreamer
    {
        public BlobStreamer(string azureStorageConnectionString, ILogger logger) : base(azureStorageConnectionString, logger, "Stream")
        {
        }

        public async Task<Stream> GetBlobContents(string containerName, string blobName)
        {
            var container = GetBlobContainer(containerName);
            var blob = container.GetBlobClient(blobName);
            return await blob.OpenReadAsync();
        }
    }
}