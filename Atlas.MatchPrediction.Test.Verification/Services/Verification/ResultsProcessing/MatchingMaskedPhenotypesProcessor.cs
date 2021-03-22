using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Services.Verification.ResultsProcessing.Storers;

namespace Atlas.MatchPrediction.Test.Verification.Services.Verification.ResultsProcessing
{
    internal class MatchingMaskedPhenotypesProcessor : ResultSetProcessor<SearchResultsNotification, OriginalSearchResultSet, SearchResult>
    {
        private readonly ISimulantChecker simulantChecker;
        private readonly IResultsStorer<SearchResult, MatchedDonor> donorsStorer;
        private readonly IResultsStorer<SearchResult, LocusMatchCount> countsStorer;
        private readonly IResultsStorer<SearchResult, MatchProbability> probabilitiesStorer;

        public MatchingMaskedPhenotypesProcessor(
            ISearchRequestsRepository searchRequestsRepository,
            ISearchResultsStreamer resultsStreamer,
            ISimulantChecker simulantChecker,
            IResultsStorer<SearchResult, MatchedDonor> donorsStorer,
            IResultsStorer<SearchResult, LocusMatchCount> countsStorer,
            IResultsStorer<SearchResult, MatchProbability> probabilitiesStorer)
        : base(searchRequestsRepository, resultsStreamer)
        {
            this.simulantChecker = simulantChecker;
            this.donorsStorer = donorsStorer;
            this.countsStorer = countsStorer;
            this.probabilitiesStorer = probabilitiesStorer;
        }

        /// <returns>`true` if result was for a Masked simulant, else `false`</returns>
        protected override async Task<bool> ProcessAndStoreResults(SearchRequestRecord searchRequest, OriginalSearchResultSet resultSet)
        {
            if (await simulantChecker.IsPatientAGenotypeSimulant(searchRequest.VerificationRun_Id, searchRequest.PatientSimulant_Id))
            {
                return false;
            }

            await donorsStorer.ProcessAndStoreResults(searchRequest.Id, resultSet);
            await countsStorer.ProcessAndStoreResults(searchRequest.Id, resultSet);
            await probabilitiesStorer.ProcessAndStoreResults(searchRequest.Id, resultSet);

            return true;
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