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
        Task ProcessAndStoreSearchResultSet(SearchResultsNotification notification);
    }

    internal class SearchResultSetSetProcessor : ISearchResultSetProcessor
    {
        private readonly ISearchRequestsRepository searchRequestsRepository;
        private readonly ISearchResultsStreamer resultsStreamer;
        private readonly IResultsProcessor<MatchedDonor> donorsProcessor;
        private readonly IResultsProcessor<LocusMatchCount> countsProcessor;
        private readonly IResultsProcessor<MatchProbability> probabilitiesProcessor;

        public SearchResultSetSetProcessor(
            ISearchRequestsRepository searchRequestsRepository,
            ISearchResultsStreamer resultsStreamer,
            IResultsProcessor<MatchedDonor> donorsProcessor,
            IResultsProcessor<LocusMatchCount> countsProcessor,
            IResultsProcessor<MatchProbability> probabilitiesProcessor)
        {
            this.searchRequestsRepository = searchRequestsRepository;
            this.resultsStreamer = resultsStreamer;
            this.donorsProcessor = donorsProcessor;
            this.countsProcessor = countsProcessor;
            this.probabilitiesProcessor = probabilitiesProcessor;
        }

        public async Task ProcessAndStoreSearchResultSet(SearchResultsNotification notification)
        {
            var recordId = await searchRequestsRepository.GetRecordIdByAtlasSearchId(notification.SearchRequestId);

            if (recordId == 0)
            {
                Debug.WriteLine($"No record found with Atlas search id {notification.SearchRequestId}.");
                return;
            }

            if (!notification.WasSuccessful)
            {
                await searchRequestsRepository.MarkSearchResultsAsFailed(recordId);
                Debug.WriteLine($"Search request {recordId} was not successful - record updated.");
                return;
            }

            await FetchAndPersistResults(recordId, notification);
        }

        private async Task FetchAndPersistResults(int recordId, SearchResultsNotification notification)
        {
            var resultSet = JsonConvert.DeserializeObject<SearchResultSet>(await DownloadResults(notification));
            await ProcessAndStoreResults(recordId, resultSet);
            await searchRequestsRepository.MarkSearchResultsAsSuccessful(GetSuccessInfo(recordId, notification));

            Debug.WriteLine($"Search request {recordId} was successful - {resultSet.TotalResults} matched donors found.");
        }

        private async Task<string> DownloadResults(SearchResultsNotification notification)
        {
            var blobStream = await resultsStreamer.GetSearchResultsBlobContents(
                notification.BlobStorageContainerName, notification.ResultsFileName);
            return await new StreamReader(blobStream).ReadToEndAsync();
        }

        private async Task ProcessAndStoreResults(int searchRequestRecordId, SearchResultSet resultSet)
        {
            await donorsProcessor.ProcessAndStoreResults(searchRequestRecordId, resultSet);
            await countsProcessor.ProcessAndStoreResults(searchRequestRecordId, resultSet);
            await probabilitiesProcessor.ProcessAndStoreResults(searchRequestRecordId, resultSet);
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