using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Common.AzureStorage.Blob;
using Atlas.ManualTesting.Common.Models;
using Atlas.ManualTesting.Common.Models.Entities;
using Atlas.ManualTesting.Common.Repositories;
using Atlas.ManualTesting.Common.Services;
using Atlas.ManualTesting.Common.Services.Storers;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification;
using Atlas.MatchPrediction.Test.Verification.Services.Verification.ResultsProcessing.Storers;

namespace Atlas.MatchPrediction.Test.Verification.Services.Verification.ResultsProcessing
{
    internal class SearchResultSetProcessor : ResultSetProcessor<SearchResultsNotification, OriginalSearchResultSet, SearchResult, VerificationSearchRequestRecord>
    {
        private readonly IResultsStorer<SearchResult, MatchedDonor> donorsStorer;
        private readonly IResultsStorer<SearchResult, LocusMatchDetails> countsStorer;
        private readonly IMismatchedDonorsStorer<SearchResult> mismatchedDonorsStorer;
        private readonly IResultsStorer<SearchResult, MatchedDonorProbability> probabilitiesStorer;

        public SearchResultSetProcessor(
            ISearchRequestsRepository<VerificationSearchRequestRecord> searchRequestsRepository,
            IBlobStreamer resultsStreamer,
            IResultsStorer<SearchResult, MatchedDonor> donorsStorer,
            IResultsStorer<SearchResult, LocusMatchDetails> countsStorer,
            IMismatchedDonorsStorer<SearchResult> mismatchedDonorsStorer,
            IResultsStorer<SearchResult, MatchedDonorProbability> probabilitiesStorer)
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
        protected override bool ShouldProcessResult(VerificationSearchRequestRecord searchRequest)
        {
            return searchRequest.WasMatchPredictionRun;
        }

        protected override async Task ProcessAndStoreResults(VerificationSearchRequestRecord searchRequest, OriginalSearchResultSet resultSet)
        {
            await donorsStorer.ProcessAndStoreResults(searchRequest.Id, resultSet);
            await countsStorer.ProcessAndStoreResults(searchRequest.Id, resultSet);
            await mismatchedDonorsStorer.CreateRecordsForGenotypeDonorsWithTooManyMismatches(searchRequest, resultSet);
            await probabilitiesStorer.ProcessAndStoreResults(searchRequest.Id, resultSet);
        }

        protected override SuccessfulSearchRequestInfo GetSuccessInfo(int searchRequestRecordId, int numberOfResults)
        {
            return new SuccessfulSearchRequestInfo
            {
                SearchRequestRecordId = searchRequestRecordId,
                MatchedDonorCount = numberOfResults
            };
        }
    }
}