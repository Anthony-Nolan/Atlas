using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Azure.Storage.Blobs;

namespace Atlas.Common.AzureStorage.Blob
{
    public abstract class AzureStorageBlobClient
    {
        // Azure SDK clients are thread-safe and hold no connections of their own, so one client per storage account can serve the
        // whole process. Most instances of this class are made per scope, and a process can use more than one storage account.
        // Lazy makes only one client per connection string, also when two threads ask for it at the same time.
        private static readonly ConcurrentDictionary<string, Lazy<BlobServiceClient>> ServiceClientsByConnectionString = new();

        protected readonly IAtlasLogger logger;
        private readonly string logLabel;

        protected AzureStorageBlobClient(string azureStorageConnectionString, IAtlasLogger logger, string logLabel)
        {
            ArgumentNullException.ThrowIfNull(azureStorageConnectionString);
            ServiceClient = ServiceClientsByConnectionString
                .GetOrAdd(azureStorageConnectionString,
                    connectionString => new Lazy<BlobServiceClient>(() => new BlobServiceClient(connectionString)))
                .Value;
            this.logger = logger;
            this.logLabel = logLabel;
        }

        internal BlobServiceClient ServiceClient { get; }

        /// <summary>
        ///  Gets blob container reference without any check to see if it exists or not.
        /// </summary>
        protected BlobContainerClient GetBlobContainer(string containerName)
        {
            return ServiceClient.GetBlobContainerClient(containerName);
        }

        /// <summary>
        /// Will first create the container if it doesn't exist, and then returns the container.
        /// </summary>
        protected async Task<BlobContainerClient> CreateAndGetBlobContainer(string containerName)
        {
            var container = ServiceClient.GetBlobContainerClient(containerName);

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