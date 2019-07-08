using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Storage;

namespace Nova.SearchAlgorithm.Services.AzureStorage
{
    public interface IResultsBlobStorageClient
    {
        Task UploadResults(string requestId, SearchResultSet searchResultSet);
        string GetResultsContainerName();
    }

    public class ResultsBlobStorageClient : AzureStorageBlobClient, IResultsBlobStorageClient
    {
        private readonly string resultsContainerName;

        public ResultsBlobStorageClient(string azureStorageConnectionString, ILogger logger, string resultsContainerName) : base(
            azureStorageConnectionString, logger)
        {
            this.resultsContainerName = resultsContainerName;
        }

        public async Task UploadResults(string requestId, SearchResultSet searchResultSet)
        {
            var serialisedResults = JsonConvert.SerializeObject(searchResultSet);
            await Upload(resultsContainerName, requestId, serialisedResults);
        }

        public string GetResultsContainerName()
        {
            return resultsContainerName;
        }
    }
}