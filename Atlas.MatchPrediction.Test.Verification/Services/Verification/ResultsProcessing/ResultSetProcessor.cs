using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.Test.Verification.Services.Verification.ResultsProcessing
{
    public interface IResultSetProcessor<in TNotification> where TNotification : IResultsNotification
    {
        Task ProcessAndStoreResultSet(TNotification notification);
    }

    internal abstract class ResultSetProcessor<TNotification, TResultSet, TResult> : IResultSetProcessor<TNotification>
        where TNotification : IResultsNotification
        where TResultSet : IResultSet<TResult>
        where TResult : IResult
    {
        private readonly ISearchRequestsRepository searchRequestsRepository;
        private readonly ISearchResultsStreamer resultsStreamer;

        protected ResultSetProcessor(
            ISearchRequestsRepository searchRequestsRepository,
            ISearchResultsStreamer resultsStreamer)
        {
            this.searchRequestsRepository = searchRequestsRepository;
            this.resultsStreamer = resultsStreamer;
        }

        public async Task ProcessAndStoreResultSet(TNotification notification)
        {
            var record = await searchRequestsRepository.GetRecordByAtlasSearchId(notification.SearchRequestId);

            if (record == null)
            {
                Debug.WriteLine($"No record found with Atlas search id {notification.SearchRequestId}.");
                return;
            }

            if (!notification.WasSuccessful)
            {
                await searchRequestsRepository.MarkSearchResultsAsFailed(record.Id);
                Debug.WriteLine($"Search request {record.Id} was not successful - record updated.");
                return;
            }

            await FetchAndPersistResults(record, notification);
        }

        private async Task FetchAndPersistResults(SearchRequestRecord searchRequest, TNotification notification)
        {
            var resultSet = JsonConvert.DeserializeObject<TResultSet>(await DownloadResults(notification));
            var wasProcessed = await ProcessAndStoreResults(searchRequest, resultSet);

            if (!wasProcessed)
            {
                return;
            }

            await searchRequestsRepository.MarkSearchResultsAsSuccessful(GetSuccessInfo(searchRequest.Id, notification));
            Debug.WriteLine($"Search request {searchRequest.Id} was successful - {resultSet.TotalResults} matched donors found.");
        }

        private async Task<string> DownloadResults(TNotification notification)
        {
            var blobStream = await resultsStreamer.GetSearchResultsBlobContents(
                notification.BlobStorageContainerName, notification.ResultsFileName);
            return await new StreamReader(blobStream).ReadToEndAsync();
        }

        /// <returns>`true` if result was processed; `false` if result was skipped.</returns>
        protected abstract Task<bool> ProcessAndStoreResults(SearchRequestRecord searchRequest, TResultSet resultSet);

        protected abstract SuccessfulSearchRequestInfo GetSuccessInfo(int searchRequestRecordId, TNotification notification);
    }
}