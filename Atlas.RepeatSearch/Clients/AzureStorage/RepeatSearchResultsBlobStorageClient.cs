using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Common.AzureStorage.Blob;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.RepeatSearch.Settings.Azure;
using Newtonsoft.Json;

namespace Atlas.RepeatSearch.Clients.AzureStorage
{
    public interface IRepeatSearchResultsBlobStorageClient
    {
        Task UploadResults(BatchedResultSet<MatchingAlgorithmResult> repeatSearchResultSet);
        string GetResultsContainerName();
    }

    public class RepeatSearchResultsBlobStorageClient : BlobUploader, IRepeatSearchResultsBlobStorageClient
    {
        private readonly AzureStorageSettings azureStorageSettings;

        // ReSharper disable once SuggestBaseTypeForParameter
        public RepeatSearchResultsBlobStorageClient(AzureStorageSettings azureStorageSettings, IMatchingAlgorithmSearchLogger logger)
            : base(azureStorageSettings.ConnectionString, logger)
        {
            this.azureStorageSettings = azureStorageSettings;
        }

        public async Task UploadResults(BatchedResultSet<MatchingAlgorithmResult> repeatSearchResultSet)
        {
            repeatSearchResultSet.BatchedResult = azureStorageSettings.ResultBatched;
            var serialisedResults = JsonConvert.SerializeObject(repeatSearchResultSet);
            await Upload(azureStorageSettings.MatchingResultsBlobContainer, repeatSearchResultSet.ResultsFileName, serialisedResults);
        }

        public string GetResultsContainerName()
        {
            return azureStorageSettings.MatchingResultsBlobContainer;
        }
    }
}
