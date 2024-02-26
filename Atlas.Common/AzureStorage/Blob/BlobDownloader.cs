using Atlas.Common.ApplicationInsights;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using Azure.Storage.Blobs;
using System.Linq;
using MoreLinq;
using Azure;

namespace Atlas.Common.AzureStorage.Blob
{
    public interface IBlobDownloader
    {
        /// <summary>
        /// Downloads a single file with the name <paramref name="filename"/> from blob container <paramref name="container"/>
        /// </summary>
        /// <typeparam name="T">Type of the entry stored in the file</typeparam>
        /// <param name="container">Blob container</param>
        /// <param name="filename">Name of the file to download</param>
        /// <returns>A single object of type <typeparamref name="T"></returns>
        Task<T> Download<T>(string container, string filename);

        /// <summary>
        /// Downloads all files within the folder <paramref name="folderName"/> from blob container <paramref name="container"/>
        /// </summary>
        /// <typeparam name="T">Type of the entries stored in the file</typeparam>
        /// <param name="container">Blob container</param>
        /// <param name="folderName">Name of the folder to download files from</param>
        /// <returns>A collection of objects of type <typeparamref name="T"></returns>
        Task<IEnumerable<T>> DownloadFolderContents<T>(string container, string folderName);

        /// <summary>
        /// Dowload files from all the locations specified in <paramref name="locations"/> from blob container <paramref name="container"/>
        /// </summary>
        /// <typeparam name="T">Type of the entries stored in the file</typeparam>
        /// <param name="container">Blob container</param>
        /// <param name="locations">A dictionary where Key is an id of the entry and Value is a file name where this entry is stored</param>
        /// <param name="batchSize">Batch size</param>
        /// <returns>A dictionary where Key is an id of the entry and Value is an object of type <typeparamref name="T"></returns>
        Task<Dictionary<int, T>> DownloadMultipleBlobs<T>(string container, IReadOnlyDictionary<int, string> locations, int batchSize);

        /// <summary>
        /// Downloads all files within the folder <paramref name="folderName"/> from blob container <paramref name="container"/> file by file (i.e. it deserializes and keeps in memory data from one file only)
        /// </summary>
        /// <typeparam name="T">Type of the entries stored in the file</typeparam>
        /// <param name="container">Blob container</param>
        /// <param name="folderName">Name of the folder to download files from</param>
        /// <returns>An enumerator with collection of objects of type <typeparamref name="T"></returns>
        IAsyncEnumerable<IEnumerable<T>> DownloadFolderContentsFileByFile<T>(string container, string folderName);
    }
    
    public class BlobDownloader : AzureStorageBlobClient, IBlobDownloader
    {
        public BlobDownloader(string azureStorageConnectionString, ILogger logger) : base(azureStorageConnectionString, logger, "Download")
        {
        }

        public async Task<T> Download<T>(string container, string filename)
        {
            var azureStorageEventModel = StartAzureStorageCommunication(filename, container);

            var containerClient = GetBlobContainer(container);
            var data = await GetBlobData<T>(containerClient, filename);

            EndAzureStorageCommunication(azureStorageEventModel);

            return data;
        }

        public async Task<IEnumerable<T>> DownloadFolderContents<T>(string container, string folderName)
        {
            var data = new List<T>();

            var azureStorageEventModel = StartAzureStorageCommunication(folderName, container);

            var containerClient = GetBlobContainer(container);
            var blobs =  containerClient.GetBlobsAsync(prefix: $"{folderName}/");

            await foreach (var blob in blobs)
            {
                data.AddRange(await GetBlobData<IEnumerable<T>>(containerClient, blob.Name));
            }

            EndAzureStorageCommunication(azureStorageEventModel);

            return data;
        }

        public async Task<Dictionary<int, T>> DownloadMultipleBlobs<T>(string container, IReadOnlyDictionary<int, string> locations, int batchSize)
        {
            var data = new Dictionary<int, T>();

            var azureStorageEventModel = StartAzureStorageCommunication(container, container);

            var containerClient = GetBlobContainer(container);

            foreach (var locationBatch in locations.Batch(batchSize))
            {
                var getBlobDataTasksDictionary = Enumerable.ToDictionary(locationBatch, location => location.Key,
                    location => Task.Run(() => GetBlobData<T>(containerClient, location.Value)));
                await Task.WhenAll(getBlobDataTasksDictionary.Values);
                locationBatch.ForEach(location => { data[location.Key] = getBlobDataTasksDictionary[location.Key].Result; });
            }

            EndAzureStorageCommunication(azureStorageEventModel);

            return data;
        }

        public async IAsyncEnumerable<IEnumerable<T>> DownloadFolderContentsFileByFile<T>(string container, string folderName)
        {
            var azureStorageEventModel = StartAzureStorageCommunication(folderName, container);

            var containerClient = GetBlobContainer(container);
            var blobs = containerClient.GetBlobsAsync(prefix: $"{folderName}/");

            await foreach (var blob in blobs)
            {
                yield return await GetBlobData<IEnumerable<T>>(containerClient, blob.Name);
            }

            EndAzureStorageCommunication(azureStorageEventModel);
        }

        private static async Task<T> GetBlobData<T>(BlobContainerClient containerClient, string filename)
        {
            try
            {
                var blobClient = containerClient.GetBlobClient(filename);
                var downloadedBlob = await blobClient.DownloadContentAsync();

                if (downloadedBlob is not { HasValue: true })
                {
                    throw new BlobNotFoundException(containerClient.Name, filename);
                }

                return JsonConvert.DeserializeObject<T>(downloadedBlob.Value.Content.ToString());
            }
            catch (RequestFailedException ex) when(ex.ErrorCode == "BlobNotFound")
            {
                throw new BlobNotFoundException(containerClient.Name, filename);
            }
        }
    }
}