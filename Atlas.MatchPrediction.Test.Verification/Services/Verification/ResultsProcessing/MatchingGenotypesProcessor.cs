using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Services.Verification.ResultsProcessing.Storers;

namespace Atlas.MatchPrediction.Test.Verification.Services.Verification.ResultsProcessing
{
    internal class MatchingGenotypesProcessor : ResultSetProcessor<MatchingResultsNotification, OriginalMatchingAlgorithmResultSet, MatchingAlgorithmResult>
    {
        private readonly ISimulantChecker simulantChecker;
        private readonly IResultsStorer<MatchingAlgorithmResult, MatchedDonor> donorsStorer;
        private readonly IResultsStorer<MatchingAlgorithmResult, LocusMatchCount> countsStorer;
        private readonly IMismatchedDonorsStorer mismatchedDonorsStorer;

        public MatchingGenotypesProcessor(
            ISearchRequestsRepository searchRequestsRepository,
            ISearchResultsStreamer resultsStreamer,
            ISimulantChecker simulantChecker,
            IResultsStorer<MatchingAlgorithmResult,MatchedDonor> donorsStorer,
            IResultsStorer<MatchingAlgorithmResult,LocusMatchCount> countsStorer,
            IMismatchedDonorsStorer mismatchedDonorsStorer)
        : base(searchRequestsRepository, resultsStreamer)
        {
            this.simulantChecker = simulantChecker;
            this.donorsStorer = donorsStorer;
            this.countsStorer = countsStorer;
            this.mismatchedDonorsStorer = mismatchedDonorsStorer;
        }

        /// <returns>`true` if result was for a Genotype simulant, else `false`</returns>
        protected override async Task<bool> ProcessAndStoreResults(SearchRequestRecord searchRequest, OriginalMatchingAlgorithmResultSet resultSet)
        {
            if (!await simulantChecker.IsPatientAGenotypeSimulant(searchRequest.VerificationRun_Id, searchRequest.PatientSimulant_Id))
            {
                return false;
            }

            await donorsStorer.ProcessAndStoreResults(searchRequest.Id, resultSet);
            await countsStorer.ProcessAndStoreResults(searchRequest.Id, resultSet);
            await mismatchedDonorsStorer.CreateRecordsForGenotypeDonorsWithTooManyMismatches(searchRequest, resultSet);

            return true;
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