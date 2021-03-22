using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.Verification;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;

namespace Atlas.MatchPrediction.Test.Verification.Services.Verification.ResultsProcessing.Storers
{
    internal class MaskedMatchCountsStorer : MatchCountsStorer<SearchResult>
    {
        public MaskedMatchCountsStorer(
            IProcessedResultsRepository<LocusMatchCount> resultsRepository,
            IMatchedDonorsRepository matchedDonorsRepository)
            : base(resultsRepository, matchedDonorsRepository)
        {
        }

        protected override string GetDonorCode(SearchResult result)
        {
            return result.DonorCode;
        }

        protected override ScoringResult GetScoringResult(SearchResult result)
        {
            return result.MatchingResult.ScoringResult;
        }
    }
}