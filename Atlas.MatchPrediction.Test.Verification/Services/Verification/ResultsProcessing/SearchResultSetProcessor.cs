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
    public interface ISearchResultSetProcessor
    {
        /// <summary>
        /// Note: Only loci match counts with value > 0 will be stored.
        /// </summary>
        Task ProcessAndStoreSearchResultSet(SearchResultsNotification notification);
    }

    internal class SearchResultSetSetProcessor : ISearchResultSetProcessor
    {
        private readonly ISearchRequestsRepository searchRequestsRepository;
        private readonly ISearchResultsStreamer resultsStreamer;
        private readonly IResultsProcessor<MatchedDonor> donorsProcessor;
        private readonly IResultsProcessor<LocusMatchCount> countsProcessor;
        private readonly IResultsProcessor<MatchProbability> probabilitiesProcessor;
        private readonly IMismatchedDonorsProcessor mismatchedDonorsProcessor;

        public SearchResultSetSetProcessor(
            ISearchRequestsRepository searchRequestsRepository,
            ISearchResultsStreamer resultsStreamer,
            IResultsProcessor<MatchedDonor> donorsProcessor,
            IResultsProcessor<LocusMatchCount> countsProcessor,
            IResultsProcessor<MatchProbability> probabilitiesProcessor,
            IMismatchedDonorsProcessor mismatchedDonorsProcessor)
        {
            this.searchRequestsRepository = searchRequestsRepository;
            this.resultsStreamer = resultsStreamer;
            this.donorsProcessor = donorsProcessor;
            this.countsProcessor = countsProcessor;
            this.probabilitiesProcessor = probabilitiesProcessor;
            this.mismatchedDonorsProcessor = mismatchedDonorsProcessor;
        }

        public async Task ProcessAndStoreSearchResultSet(SearchResultsNotification notification)
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

        private async Task FetchAndPersistResults(SearchRequestRecord searchRequest, SearchResultsNotification notification)
        {
            var resultSet = JsonConvert.DeserializeObject<SearchResultSet>(await DownloadResults(notification));
            await ProcessAndStoreResults(searchRequest, resultSet);
            await searchRequestsRepository.MarkSearchResultsAsSuccessful(GetSuccessInfo(searchRequest.Id, notification));

            Debug.WriteLine($"Search request {searchRequest.Id} was successful - {resultSet.TotalResults} matched donors found.");
        }

        private async Task<string> DownloadResults(SearchResultsNotification notification)
        {
            var blobStream = await resultsStreamer.GetSearchResultsBlobContents(
                notification.BlobStorageContainerName, notification.ResultsFileName);
            return await new StreamReader(blobStream).ReadToEndAsync();
        }

        private async Task ProcessAndStoreResults(SearchRequestRecord searchRequest, SearchResultSet resultSet)
        {
            await donorsProcessor.ProcessAndStoreResults(searchRequest.Id, resultSet);
            await countsProcessor.ProcessAndStoreResults(searchRequest.Id, resultSet);
            await probabilitiesProcessor.ProcessAndStoreResults(searchRequest.Id, resultSet);
            await mismatchedDonorsProcessor.CreateRecordsForGenotypeDonorsWithTooManyMismatches(searchRequest, resultSet);
        }

        private static SuccessfulSearchRequestInfo GetSuccessInfo(int searchRequestRecordId, SearchResultsNotification notification)
        {
            return new SuccessfulSearchRequestInfo
            {
                SearchRequestRecordId = searchRequestRecordId,
                MatchedDonorCount = notification.NumberOfResults,
                MatchingAlgorithmTimeInMs = notification.MatchingAlgorithmTime.TotalMilliseconds,
                MatchPredictionTimeInMs = notification.MatchPredictionTime.TotalMilliseconds,
                OverallSearchTimeInMs = notification.OverallSearchTime.TotalMilliseconds
            };
        }
    }
}