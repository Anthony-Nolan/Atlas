using Atlas.Client.Models.Search.Results.Matching;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;

namespace Atlas.MatchPrediction.Test.Verification.Services.Verification.ResultsProcessing.Storers
{
    internal class GenotypeMatchCountsStorer : MatchCountsStorer<MatchingAlgorithmResult>
    {
        public GenotypeMatchCountsStorer(
            IProcessedResultsRepository<LocusMatchCount> resultsRepository,
            IMatchedDonorsRepository matchedDonorsRepository)
            : base(resultsRepository, matchedDonorsRepository)
        {
        }

        protected override string GetDonorCode(MatchingAlgorithmResult result)
        {
            return result.ExternalDonorCode;
        }

        protected override ScoringResult GetScoringResult(MatchingAlgorithmResult result)
        {
            return result.ScoringResult;
        }
    }
}