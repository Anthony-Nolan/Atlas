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
        /// <summary>
        /// if "batchFolder" is not null, results will be loaded from all files within this folder, otherwise - results will be loaded along with search summary from "blobName" file
        /// <summary>
        public Task<ResultSet<MatchingAlgorithmResult>> Download(string blobName, bool isRepeatSearch, string batchFolder = null);

        Task<ResultSet<MatchingAlgorithmResult>> DownloadSummary(string blobName, bool isRepeatSearch);
        IAsyncEnumerable<IEnumerable<MatchingAlgorithmResult>> DownloadResults(bool isRepeatSearch, string batchFolder);
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

        public async Task<ResultSet<MatchingAlgorithmResult>> Download(string blobName, bool isRepeatSearch, string batchFolder = null)
        {
            using (logger.RunTimed($"Downloading matching results: {blobName}"))
            {
                var matchingResults = await DownloadSummary(blobName, isRepeatSearch);

                matchingResults.Results ??= !string.IsNullOrEmpty(batchFolder)
                    ? await blobDownloader.DownloadFolderContents<MatchingAlgorithmResult>(GetBlobContainer(isRepeatSearch), batchFolder)
                    : new List<MatchingAlgorithmResult>();

                return matchingResults;
            }
        }

        public async Task<ResultSet<MatchingAlgorithmResult>> DownloadSummary(string blobName, bool isRepeatSearch)
        {
            using (logger.RunTimed($"Downloading matching results summary: {blobName}"))
            {
                var matchingResultsBlobContainer = GetBlobContainer(isRepeatSearch);
                return isRepeatSearch
                    ? await blobDownloader.Download<RepeatMatchingAlgorithmResultSet>(matchingResultsBlobContainer, blobName)
                    : await blobDownloader.Download<OriginalMatchingAlgorithmResultSet>(matchingResultsBlobContainer, blobName);
            }
        }

        public async IAsyncEnumerable<IEnumerable<MatchingAlgorithmResult>> DownloadResults(bool isRepeatSearch, string batchFolder)
        {
            await foreach (var results in blobDownloader.DownloadFolderContentsFileByFile<MatchingAlgorithmResult>(GetBlobContainer(isRepeatSearch), batchFolder))
            {
                yield return results;
            }
        }
        
        private string GetBlobContainer(bool isRepeatSearch)
            => isRepeatSearch ? azureStorageSettings.RepeatSearchMatchingResultsBlobContainer : azureStorageSettings.MatchingResultsBlobContainer;

    }
}