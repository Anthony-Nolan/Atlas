using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Client.Models.SearchResults;

namespace Nova.SearchAlgorithm.Services.AzureStorage
{
    public interface IBlobStorageClient
    {
        Task UploadResults(string requestId, SearchResultSet searchResultSet);
        string GetResultsContainerName();
    }
    
    public class BlobStorageClient : IBlobStorageClient
    {
        private readonly string azureStorageConnectionString;
        private readonly string resultsContainerName;

        public BlobStorageClient(string azureStorageConnectionString, string resultsContainerName)
        {
            this.azureStorageConnectionString = azureStorageConnectionString;
            this.resultsContainerName = resultsContainerName;
        }
        
        public async Task UploadResults(string requestId, SearchResultSet searchResultSet)
        {
            var blockBlob = await GetBlockBlob(requestId);
            var serialisedResults = JsonConvert.SerializeObject(searchResultSet);
            await blockBlob.UploadTextAsync(serialisedResults);
        }

        public string GetResultsContainerName()
        {
            return resultsContainerName;
        }

        private async Task<CloudBlockBlob> GetBlockBlob(string requestId)
        {
            var container = await GetBlobContainer();
            return container.GetBlockBlobReference(requestId);
        }

        private async Task<CloudBlobContainer> GetBlobContainer()
        {
            var blobClient = CloudStorageAccount.Parse(azureStorageConnectionString).CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(resultsContainerName);
            await container.CreateIfNotExistsAsync();
            return container;
        }
    }
}