using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Functions.Settings;
using Microsoft.Extensions.Options;

namespace Atlas.Functions.Services.BlobStorageClients
{
    public interface IMatchingResultsDownloader
    {
        public Task<ResultSet<MatchingAlgorithmResult>> Download(string blobName, bool isRepeatSearch, string batchFolder = null);
    }

    internal class MatchingResultsDownloader : IMatchingResultsDownloader
    {
        private readonly AzureStorageSettings azureStorageSettings;
        private readonly IBlobDownloader blobDownloader;
        private readonly ILogger logger;

        public MatchingResultsDownloader(IOptions<AzureStorageSettings> azureStorageSettings, IBlobDownloader blobDownloader, ILogger logger)
        {
            this.azureStorageSettings = azureStorageSettings.Value;
            this.blobDownloader = blobDownloader;
            this.logger = logger;
        }

        /// <summary>
        /// if "batchFolder" is not null, results will be loaded from all files within this folder, otherwise - results will be loaded along with search summary from "blobName" file
        /// <summary>
        public async Task<ResultSet<MatchingAlgorithmResult>> Download(string blobName, bool isRepeatSearch, string batchFolder = null)
        {
            using (logger.RunTimed($"Downloading matching results: {blobName}"))
            {
                var matchingResultsBlobContainer = isRepeatSearch
                    ? azureStorageSettings.RepeatSearchMatchingResultsBlobContainer
                    : azureStorageSettings.MatchingResultsBlobContainer;
                var matchingResults = isRepeatSearch
                    ? await blobDownloader.Download<RepeatMatchingAlgorithmResultSet>(matchingResultsBlobContainer, blobName) as ResultSet<MatchingAlgorithmResult>
                    : await blobDownloader.Download<OriginalMatchingAlgorithmResultSet>(matchingResultsBlobContainer, blobName);

                matchingResults.Results ??= !string.IsNullOrEmpty(batchFolder)
                    ? await blobDownloader.DownloadFolderContents<MatchingAlgorithmResult>(matchingResultsBlobContainer, batchFolder)
                    : new List<MatchingAlgorithmResult>();

                return matchingResults;
            }
        }

    }
}