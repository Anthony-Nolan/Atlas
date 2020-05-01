using System.Threading.Tasks;
using Newtonsoft.Json;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Atlas.Utils.Core.ApplicationInsights;
using Atlas.Utils.Storage;

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