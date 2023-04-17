using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Common.AzureStorage.Blob;
using Atlas.RepeatSearch.Data.Repositories;
using Atlas.RepeatSearch.Services.Search;

namespace Atlas.RepeatSearch.Services.ResultSetTracking
{
    public interface IOriginalSearchResultSetTracker
    {
        Task StoreOriginalSearchResults(MatchingResultsNotification notification);
        Task ApplySearchResultDiff(string searchRequestId, SearchResultDifferential searchResultDifferential);
    }

    internal class OriginalSearchResultSetTracker : IOriginalSearchResultSetTracker
    {
        private readonly IBlobDownloader blobDownloader;
        private readonly ICanonicalResultSetRepository canonicalResultSetRepository;

        public OriginalSearchResultSetTracker(IBlobDownloader blobDownloader, ICanonicalResultSetRepository canonicalResultSetRepository)
        {
            this.blobDownloader = blobDownloader;
            this.canonicalResultSetRepository = canonicalResultSetRepository;
        }

        public async Task StoreOriginalSearchResults(MatchingResultsNotification notification)
        {
            var resultSet = await blobDownloader.Download<OriginalMatchingAlgorithmResultSet>(
                notification.BlobStorageContainerName,
                notification.ResultsFileName);

            var donorIds = new List<string>();

            if (notification.NumberOfResults > 0)
            {
                if (notification.ResultsBatched && !string.IsNullOrEmpty(notification.BatchFolderName))
                {
                    var matchingAlgorithmResults = blobDownloader.DownloadFolderContentsFileByFile<MatchingAlgorithmResult>(notification.BlobStorageContainerName, notification.BatchFolderName);
                    await foreach (var resultsList in matchingAlgorithmResults)
                    {
                        donorIds.AddRange(resultsList.Select(r => r.DonorCode));
                    }
                }
                else
                {
                    donorIds = resultSet.Results.Select(r => r.DonorCode).ToList();
                }
            }

            await canonicalResultSetRepository.CreateCanonicalResultSet(resultSet.SearchRequestId, donorIds);
        }

        public async Task ApplySearchResultDiff(string searchRequestId, SearchResultDifferential searchResultDifferential)
        {
            var donorCodesToAdd = searchResultDifferential.NewResults.Select(d => d.ExternalDonorCode).ToList();

            if (donorCodesToAdd.Any())
            {
                await canonicalResultSetRepository.AddResultsToSet(searchRequestId, donorCodesToAdd);
            }

            if (searchResultDifferential.RemovedResults.Any())
            {
                await canonicalResultSetRepository.RemoveResultsFromSet(searchRequestId, searchResultDifferential.RemovedResults);
            }
        }
    }
}