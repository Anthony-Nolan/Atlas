using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Common.AzureStorage.Blob;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.RepeatSearch.Settings.Azure;
using Newtonsoft.Json;

namespace Atlas.RepeatSearch.Clients.AzureStorage
{
    public interface IRepeatSearchResultsBlobStorageClient
    {
        Task UploadResults(MatchingAlgorithmResultSet repeatSearchResultSet);
        string GetResultsContainerName();
    }

    public class RepeatSearchResultsBlobStorageClient : BlobUploader, IRepeatSearchResultsBlobStorageClient
    {
        private readonly string resultsContainerName;

        // ReSharper disable once SuggestBaseTypeForParameter
        public RepeatSearchResultsBlobStorageClient(AzureStorageSettings azureStorageSettings, IMatchingAlgorithmSearchLogger logger)
            : base(azureStorageSettings.ConnectionString, logger)
        {
            resultsContainerName = azureStorageSettings.MatchingResultsBlobContainer;
        }

        public async Task UploadResults(MatchingAlgorithmResultSet repeatSearchResultSet)
        {
            var serialisedResults = JsonConvert.SerializeObject(repeatSearchResultSet);
            await Upload(resultsContainerName, repeatSearchResultSet.ResultsFileName, serialisedResults);
        }

        public string GetResultsContainerName()
        {
            return resultsContainerName;
        }
    }
}
