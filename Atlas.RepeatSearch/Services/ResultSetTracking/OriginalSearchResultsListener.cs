using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.AzureStorage.Blob;
using Atlas.RepeatSearch.Data.Repositories;

namespace Atlas.RepeatSearch.Services.ResultSetTracking
{
    public interface IOriginalSearchResultsListener
    {
        Task StoreOriginalSearchResults(MatchingResultsNotification notification);
    }

    internal class OriginalSearchResultsListener : IOriginalSearchResultsListener
    {
        private readonly IBlobDownloader blobDownloader;
        private readonly ICanonicalResultSetRepository canonicalResultSetRepository;

        public OriginalSearchResultsListener(IBlobDownloader blobDownloader, ICanonicalResultSetRepository canonicalResultSetRepository)
        {
            this.blobDownloader = blobDownloader;
            this.canonicalResultSetRepository = canonicalResultSetRepository;
        }

        public async Task StoreOriginalSearchResults(MatchingResultsNotification notification)
        {
            var resultSet = await blobDownloader.Download<MatchingAlgorithmResultSet>(
                notification.BlobStorageContainerName,
                notification.BlobStorageResultsFileName);

            var donorIds = resultSet.MatchingAlgorithmResults.Select(r => r.AtlasDonorId).ToList();
            await canonicalResultSetRepository.CreateCanonicalResultSet(resultSet.SearchRequestId, donorIds);
        }
    }
}