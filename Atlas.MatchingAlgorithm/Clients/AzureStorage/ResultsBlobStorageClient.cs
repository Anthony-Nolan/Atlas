using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.MatchingAlgorithm.ApplicationInsights.SearchRequests;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Atlas.MatchingAlgorithm.Settings.Azure;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Clients.AzureStorage
{
    public interface IResultsBlobStorageClient
    {
        Task UploadResults(MatchingAlgorithmResultSet searchResultSet);
        string GetResultsContainerName();
    }

    public class ResultsBlobStorageClient : AzureStorageBlobClient, IResultsBlobStorageClient
    {
        private readonly string resultsContainerName;

        // ReSharper disable once SuggestBaseTypeForParameter
        public ResultsBlobStorageClient(MatchingAzureStorageSettings azureStorageSettings, IMatchingAlgorithmLogger logger)
            : base(azureStorageSettings.ConnectionString, logger)
        {
            this.resultsContainerName = azureStorageSettings.SearchResultsBlobContainer;
        }

        public async Task UploadResults(MatchingAlgorithmResultSet searchResultSet)
        {
            var serialisedResults = JsonConvert.SerializeObject(searchResultSet);
            await Upload(resultsContainerName, searchResultSet.ResultsFileName, serialisedResults);
        }

        public string GetResultsContainerName()
        {
            return resultsContainerName;
        }
    }
}