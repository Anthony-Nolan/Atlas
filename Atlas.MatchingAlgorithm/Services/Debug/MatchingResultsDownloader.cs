using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Common.AzureStorage.Blob;
using Atlas.MatchingAlgorithm.Settings.Azure;
using Microsoft.Extensions.Options;

namespace Atlas.MatchingAlgorithm.Services.Debug
{
    public interface IMatchingResultsDownloader
    {
        /// <summary>
        /// If <paramref name="batchFolder"/> is not null, results will be loaded from all files within this folder.
        /// Otherwise, results will be loaded along with search summary from <paramref name="searchResultFileName"/> file.
        /// </summary>
        public Task<OriginalMatchingAlgorithmResultSet> DownloadResultSet(string searchResultFileName, string batchFolder = null);
    }

    internal class MatchingResultsDownloader : IMatchingResultsDownloader
    {
        private readonly string searchResultContainer;
        private readonly IBlobDownloader blobDownloader;

        public MatchingResultsDownloader(IOptions<AzureStorageSettings> azureStorageSettings, IBlobDownloader blobDownloader)
        {
            searchResultContainer = azureStorageSettings.Value.SearchResultsBlobContainer;
            this.blobDownloader = blobDownloader;
        }

        public async Task<OriginalMatchingAlgorithmResultSet> DownloadResultSet(string searchResultFileName, string batchFolder = null)
        {
            var resultSet = await DownloadResultSetFile(searchResultFileName);

            resultSet.Results ??= !string.IsNullOrEmpty(batchFolder)
                ? await blobDownloader.DownloadFolderContents<MatchingAlgorithmResult>(searchResultContainer, batchFolder)
                : new List<MatchingAlgorithmResult>();

            return resultSet;
        }

        private async Task<OriginalMatchingAlgorithmResultSet> DownloadResultSetFile(string searchResultFileName)
        {
            return await blobDownloader.Download<OriginalMatchingAlgorithmResultSet>(searchResultContainer, searchResultFileName);
        }
    }
}