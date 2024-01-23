using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
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
    internal class MatchingResultSetProcessor : ResultSetProcessor<MatchingResultsNotification, OriginalMatchingAlgorithmResultSet, MatchingAlgorithmResult, VerificationSearchRequestRecord>
    {
        private readonly IResultsStorer<MatchingAlgorithmResult, MatchedDonor> donorsStorer;
        private readonly IResultsStorer<MatchingAlgorithmResult, LocusMatchDetails> countsStorer;
        private readonly IMismatchedDonorsStorer<MatchingAlgorithmResult> mismatchedDonorsStorer;

        public MatchingResultSetProcessor(
            ISearchRequestsRepository<VerificationSearchRequestRecord> searchRequestsRepository,
            IBlobStreamer resultsStreamer,
            IResultsStorer<MatchingAlgorithmResult,MatchedDonor> donorsStorer,
            IResultsStorer<MatchingAlgorithmResult,LocusMatchDetails> countsStorer,
            IMismatchedDonorsStorer<MatchingAlgorithmResult> mismatchedDonorsStorer)
        : base(searchRequestsRepository, resultsStreamer)
        {
            this.donorsStorer = donorsStorer;
            this.countsStorer = countsStorer;
            this.mismatchedDonorsStorer = mismatchedDonorsStorer;
        }

        /// <summary>
        /// Only store Matching result if no match prediction was run.
        /// </summary>
        protected override bool ShouldProcessResult(VerificationSearchRequestRecord searchRequest)
        {
            return !searchRequest.WasMatchPredictionRun;
        }

        protected override async Task ProcessAndStoreResults(VerificationSearchRequestRecord searchRequest, OriginalMatchingAlgorithmResultSet resultSet)
        {
            await donorsStorer.ProcessAndStoreResults(searchRequest.Id, resultSet);
            await countsStorer.ProcessAndStoreResults(searchRequest.Id, resultSet);
            await mismatchedDonorsStorer.CreateRecordsForGenotypeDonorsWithTooManyMismatches(searchRequest, resultSet);
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