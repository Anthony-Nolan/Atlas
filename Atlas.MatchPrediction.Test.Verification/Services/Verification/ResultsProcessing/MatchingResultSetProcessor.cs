using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Common.AzureStorage.Blob;
using Atlas.ManualTesting.Common.Models.Entities;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Services.Verification.ResultsProcessing.Storers;

namespace Atlas.MatchPrediction.Test.Verification.Services.Verification.ResultsProcessing
{
    internal class MatchingResultSetProcessor : ResultSetProcessor<MatchingResultsNotification, OriginalMatchingAlgorithmResultSet, MatchingAlgorithmResult>
    {
        private readonly IResultsStorer<MatchingAlgorithmResult, MatchedDonor> donorsStorer;
        private readonly IResultsStorer<MatchingAlgorithmResult, LocusMatchCount> countsStorer;
        private readonly IMismatchedDonorsStorer<MatchingAlgorithmResult> mismatchedDonorsStorer;

        public MatchingResultSetProcessor(
            ISearchRequestsRepository searchRequestsRepository,
            IBlobStreamer resultsStreamer,
            IResultsStorer<MatchingAlgorithmResult,MatchedDonor> donorsStorer,
            IResultsStorer<MatchingAlgorithmResult,LocusMatchCount> countsStorer,
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

        protected override SuccessfulSearchRequestInfo GetSuccessInfo(int searchRequestRecordId, MatchingResultsNotification notification)
        {
            return new SuccessfulSearchRequestInfo
            {
                SearchRequestRecordId = searchRequestRecordId,
                MatchedDonorCount = notification.NumberOfResults
            };
        }
    }
}