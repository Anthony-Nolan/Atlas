using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.SearchResults;

namespace Nova.SearchAlgorithm.Test.Builders
{
    public class MatchResultBuilder
    {
        private readonly MatchResult matchResult;

        public MatchResultBuilder()
        {
            matchResult = new MatchResult();
        }

        public MatchResultBuilder WithMatchCountAtLocus(Locus locus, int matchCount)
        {
            matchResult.SetMatchDetailsForLocus(locus, new LocusMatchDetails
            {
                IsLocusTyped = true,
                MatchCount = matchCount
            });
            return this;
        }
        
        public MatchResult Build()
        {
            return matchResult;
        }
    }
}