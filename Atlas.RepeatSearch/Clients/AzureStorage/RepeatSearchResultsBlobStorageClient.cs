using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Common.AzureStorage.Blob;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.RepeatSearch.Settings.Azure;
using Newtonsoft.Json;

namespace Atlas.RepeatSearch.Clients.AzureStorage
{
    public interface IRepeatSearchResultsBlobStorageClient
    {
        Task UploadResults(RepeatMatchingAlgorithmResultSet repeatSearchResultSet);
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

        public async Task UploadResults(RepeatMatchingAlgorithmResultSet repeatSearchResultSet)
        {
            repeatSearchResultSet.BatchedResult = azureStorageSettings.ShouldBatchResults;
            // Results will not be serialised if results are being batched
            var serialisedResults = JsonConvert.SerializeObject(repeatSearchResultSet);
            await Upload(azureStorageSettings.MatchingResultsBlobContainer, repeatSearchResultSet.ResultsFileName, serialisedResults);

            if (azureStorageSettings.ShouldBatchResults)
            {
                await BatchUpload(repeatSearchResultSet.Results, azureStorageSettings.SearchResultsBatchSize, azureStorageSettings.MatchingResultsBlobContainer, $"{repeatSearchResultSet.SearchRequestId}/{repeatSearchResultSet.RepeatSearchId}");
            }
        }

        public string GetResultsContainerName()
        {
            return azureStorageSettings.MatchingResultsBlobContainer;
        }
    }
}
