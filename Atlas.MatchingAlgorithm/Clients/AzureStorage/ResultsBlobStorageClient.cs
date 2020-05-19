using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Clients.AzureStorage
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