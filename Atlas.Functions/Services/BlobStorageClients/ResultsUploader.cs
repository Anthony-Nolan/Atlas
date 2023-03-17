using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Functions.Settings;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Atlas.Functions.Services.BlobStorageClients
{
    public interface IResultsUploader
    {
        Task UploadResults(SearchResultSet searchResultSet, string batchFolder);
    }

    internal class ResultsUploader : BlobUploader, IResultsUploader
    {
        private readonly AzureStorageSettings azureStorageSettings;

        /// <inheritdoc />
        public ResultsUploader(IOptions<AzureStorageSettings> azureStorageSettings, ILogger logger)
            : base(azureStorageSettings.Value.MatchingConnectionString, logger)
        {
            this.azureStorageSettings = azureStorageSettings.Value;
        }

        /// <inheritdoc />
        public async Task UploadResults(SearchResultSet searchResultSet, string batchFolder)
        {
            using (Logger.RunTimed($"Uploading results: {searchResultSet.SearchRequestId}"))
            {
                searchResultSet.BatchedResult = azureStorageSettings.ResultBatched;
                var serialisedResults = JsonConvert.SerializeObject(searchResultSet);
                var container = searchResultSet.IsRepeatSearchSet ? azureStorageSettings.RepeatSearchResultsBlobContainer : azureStorageSettings.SearchResultsBlobContainer;
                await Upload(container, searchResultSet.ResultsFileName, serialisedResults);

                if (azureStorageSettings.ResultBatched)
                {
                    await BatchUpload(searchResultSet.Results, azureStorageSettings.BatchSize, container, batchFolder);
                }
            }
        }
    }
}