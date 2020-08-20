using Atlas.Common.AzureStorage.Blob;
using Atlas.MatchingAlgorithm.Settings.Azure;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;

namespace Atlas.MatchingAlgorithm.Clients.AzureStorage
{
    public interface IResultsBlobStorageClient
    {
        Task UploadResults(MatchingAlgorithmResultSet searchResultSet);
        string GetResultsContainerName();
    }

    public class ResultsBlobStorageClient : BlobUploader, IResultsBlobStorageClient
    {
        private readonly string resultsContainerName;

        // ReSharper disable once SuggestBaseTypeForParameter
        public ResultsBlobStorageClient(AzureStorageSettings azureStorageSettings, IMatchingAlgorithmSearchLogger logger)
            : base(azureStorageSettings.ConnectionString, logger)
        {
            resultsContainerName = azureStorageSettings.SearchResultsBlobContainer;
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