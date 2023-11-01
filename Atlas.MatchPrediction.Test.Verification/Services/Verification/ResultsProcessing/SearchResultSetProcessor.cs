using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Common.AzureStorage.Blob;
using Atlas.ManualTesting.Common.Models.Entities;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Services.Verification.ResultsProcessing.Storers;

namespace Atlas.MatchPrediction.Test.Verification.Services.Verification.ResultsProcessing
{
    internal class SearchResultSetProcessor : ResultSetProcessor<SearchResultsNotification, OriginalSearchResultSet, SearchResult>
    {
        private readonly IResultsStorer<SearchResult, MatchedDonor> donorsStorer;
        private readonly IResultsStorer<SearchResult, LocusMatchCount> countsStorer;
        private readonly IMismatchedDonorsStorer<SearchResult> mismatchedDonorsStorer;
        private readonly IResultsStorer<SearchResult, MatchProbability> probabilitiesStorer;

        public SearchResultSetProcessor(
            ISearchRequestsRepository searchRequestsRepository,
            IBlobStreamer resultsStreamer,
            IResultsStorer<SearchResult, MatchedDonor> donorsStorer,
            IResultsStorer<SearchResult, LocusMatchCount> countsStorer,
            IMismatchedDonorsStorer<SearchResult> mismatchedDonorsStorer,
            IResultsStorer<SearchResult, MatchProbability> probabilitiesStorer)
        : base(searchRequestsRepository, resultsStreamer)
        {
            this.donorsStorer = donorsStorer;
            this.countsStorer = countsStorer;
            this.mismatchedDonorsStorer = mismatchedDonorsStorer;
            this.probabilitiesStorer = probabilitiesStorer;
        }

        /// <summary>
        /// Only store Search result if match prediction was run.
        /// </summary>
        protected override bool ShouldProcessResult(SearchRequestRecord searchRequest)
        {
            return searchRequest.WasMatchPredictionRun;
        }

        protected override async Task ProcessAndStoreResults(SearchRequestRecord searchRequest, OriginalSearchResultSet resultSet)
        {
            await donorsStorer.ProcessAndStoreResults(searchRequest.Id, resultSet);
            await countsStorer.ProcessAndStoreResults(searchRequest.Id, resultSet);
            await mismatchedDonorsStorer.CreateRecordsForGenotypeDonorsWithTooManyMismatches(searchRequest, resultSet);
            await probabilitiesStorer.ProcessAndStoreResults(searchRequest.Id, resultSet);
        }

        protected override SuccessfulSearchRequestInfo GetSuccessInfo(int searchRequestRecordId, SearchResultsNotification notification)
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