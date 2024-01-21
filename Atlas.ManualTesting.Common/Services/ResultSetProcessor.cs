using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Common.AzureStorage.Blob;
using Atlas.ManualTesting.Common.Models;
using Atlas.ManualTesting.Common.Models.Entities;
using Atlas.ManualTesting.Common.Repositories;
using Newtonsoft.Json;

namespace Atlas.ManualTesting.Common.Services
{
    public interface IResultSetProcessor<in TNotification> where TNotification : ResultsNotification
    {
        Task ProcessAndStoreResultSet(TNotification notification);
    }

    public abstract class ResultSetProcessor<TNotification, TResultSet, TResult, TRecord> : IResultSetProcessor<TNotification>
        where TNotification : ResultsNotification
        where TResultSet : ResultSet<TResult>
        where TResult : Result
        where TRecord : SearchRequestRecord
    {
        private readonly ISearchRequestsRepository<TRecord> searchRequestsRepository;
        private readonly IBlobStreamer resultsStreamer;

        protected ResultSetProcessor(
            ISearchRequestsRepository<TRecord> searchRequestsRepository,
            IBlobStreamer resultsStreamer)
        {
            this.searchRequestsRepository = searchRequestsRepository;
            this.resultsStreamer = resultsStreamer;
        }

        public async Task ProcessAndStoreResultSet(TNotification notification)
        {
            var record = await searchRequestsRepository.GetRecordByAtlasSearchId(notification.SearchRequestId);

            if (record == null)
            {
                System.Diagnostics.Debug.WriteLine($"No record found with Atlas search id {notification.SearchRequestId}.");
                return;
            }

            if (!ShouldProcessResult(record))
            {
                return;
            }

            if (!notification.WasSuccessful)
            {
                await searchRequestsRepository.MarkSearchResultsAsFailed(record.Id);
                System.Diagnostics.Debug.WriteLine($"Search request {record.Id} was not successful - record updated.");
                return;
            }

            await FetchAndPersistResults(record, notification);
        }

        private async Task FetchAndPersistResults(TRecord searchRequest, TNotification notification)
        {
            var resultSet = JsonConvert.DeserializeObject<TResultSet>(await DownloadResults(notification));
            await ProcessAndStoreResults(searchRequest, resultSet);
            await searchRequestsRepository.MarkSearchResultsAsSuccessful(GetSuccessInfo(searchRequest.Id, resultSet.TotalResults));
            System.Diagnostics.Debug.WriteLine($"Search request {searchRequest.Id} was successful - {resultSet.TotalResults} matched donors found.");
        }

        private async Task<string> DownloadResults(TNotification notification)
        {
            var blobStream = await resultsStreamer.GetBlobContents(
                notification.BlobStorageContainerName, notification.ResultsFileName);
            return await new StreamReader(blobStream).ReadToEndAsync();
        }

        protected abstract bool ShouldProcessResult(TRecord searchRequest);

        protected abstract Task ProcessAndStoreResults(TRecord searchRequest, TResultSet resultSet);

        protected abstract SuccessfulSearchRequestInfo GetSuccessInfo(int searchRequestRecordId, int numberOfResults);
    }
}