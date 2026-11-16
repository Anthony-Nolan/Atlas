using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Atlas.Common.AzureStorage.Blob
{
    public abstract class AzureStorageBlobClient
    {
        // Azure SDK clients are thread-safe and hold no connections of their own, so one client per storage account can serve the
        // whole process. Most instances of this class are made per scope, and a process can use more than one storage account.
        // Lazy makes only one client per connection string, also when two threads ask for it at the same time.
        private static readonly ConcurrentDictionary<string, Lazy<BlobServiceClient>> ServiceClientsByConnectionString = new();

        // Containers are made once and are not deleted while the apps run, so a process checks each container only once.
        // The key is the container URI, which names the storage account and the container.
        private static readonly ConcurrentDictionary<Uri, Lazy<Task>> ContainerChecks = new();

        protected readonly IAtlasLogger logger;
        private readonly string logLabel;

        protected AzureStorageBlobClient(string azureStorageConnectionString, IAtlasLogger logger, string logLabel)
            : this(GetServiceClient(azureStorageConnectionString), logger, logLabel)
        {
        }

        internal AzureStorageBlobClient(BlobServiceClient serviceClient, IAtlasLogger logger, string logLabel)
        {
            ServiceClient = serviceClient;
            this.logger = logger;
            this.logLabel = logLabel;
        }

        internal BlobServiceClient ServiceClient { get; }

        private static BlobServiceClient GetServiceClient(string azureStorageConnectionString)
        {
            ArgumentNullException.ThrowIfNull(azureStorageConnectionString);
            return ServiceClientsByConnectionString
                .GetOrAdd(azureStorageConnectionString,
                    connectionString => new Lazy<BlobServiceClient>(() => new BlobServiceClient(connectionString)))
                .Value;
        }

        /// <summary>
        ///  Gets blob container reference without any check to see if it exists or not.
        /// </summary>
        protected BlobContainerClient GetBlobContainer(string containerName)
        {
            return ServiceClient.GetBlobContainerClient(containerName);
        }

        /// <summary>
        /// Will first create the container if it doesn't exist, and then returns the container.
        /// Each process checks a container only on the first call: later calls use the result of that check.
        /// </summary>
        protected async Task<BlobContainerClient> CreateAndGetBlobContainer(string containerName)
        {
            var container = ServiceClient.GetBlobContainerClient(containerName);
            var check = ContainerChecks.GetOrAdd(
                container.Uri, static (_, client) => new Lazy<Task>(() => CreateContainerIfMissing(client)), container);

            try
            {
                await check.Value;
            }
            catch
            {
                // A failed check is not kept, so the next call checks the container again.
                ContainerChecks.TryRemove(KeyValuePair.Create(container.Uri, check));
                throw;
            }

            return container;
        }

        private static async Task CreateContainerIfMissing(BlobContainerClient container)
        {
            // Not CreateIfNotExistsAsync: it sends a CreateContainer request also when the container exists.
            if (await container.ExistsAsync())
            {
                return;
            }

            try
            {
                await container.CreateAsync();
            }
            catch (RequestFailedException exception) when (exception.ErrorCode == BlobErrorCode.ContainerAlreadyExists.ToString())
            {
                // Another process made the container after the check.
            }
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