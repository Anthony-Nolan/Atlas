using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Azure.Storage.Blobs;

namespace Atlas.Common.AzureStorage.Blob
{
    public abstract class AzureStorageBlobClient
    {
        private readonly BlobServiceClient blobClient;
        protected readonly IAtlasLogger logger;
        private readonly string logLabel;

        protected AzureStorageBlobClient(string azureStorageConnectionString, IAtlasLogger logger, string logLabel)
        {
            blobClient = new BlobServiceClient(azureStorageConnectionString);
            this.logger = logger;
            this.logLabel = logLabel;
        }

        /// <summary>
        ///  Gets blob container reference without any check to see if it exists or not.
        /// </summary>
        protected BlobContainerClient GetBlobContainer(string containerName)
        {
            return blobClient.GetBlobContainerClient(containerName);
        }

        /// <summary>
        /// Will first create the container if it doesn't exist, and then returns the container.
        /// </summary>
        protected async Task<BlobContainerClient> CreateAndGetBlobContainer(string containerName)
        {
            var container = blobClient.GetBlobContainerClient(containerName);

            // avoiding the use of the `CreateIfNotExists` method as it seems to send a CreateContainer API call even when the container already exists.

            if (await container.ExistsAsync())
            {
                return container;
            }

            await container.CreateAsync();
            return container;
        }

        protected async Task<T> TimedCommunication<T>(string filename, string container, Func<Task<T>> operation)
        {
            var sw = Stopwatch.StartNew();
            var result = await operation();
            sw.Stop();
            SendAzureStorageEvent(filename, container, sw.ElapsedMilliseconds);
            return result;
        }

        protected async Task TimedCommunication(string filename, string container, Func<Task> operation)
        {
            var sw = Stopwatch.StartNew();
            await operation();
            sw.Stop();
            SendAzureStorageEvent(filename, container, sw.ElapsedMilliseconds);
        }

        protected void SendAzureStorageEvent(string filename, string container, long elapsedMs)
        {
            logger.SendEvent("Azure Storage", LogLevel.Verbose,
                new Dictionary<string, string> { { "Filename", filename }, { "Container", container } },
                new Dictionary<string, double> { { $"Azure Storage - {logLabel} - duration /ms", elapsedMs } });
        }
    }
}