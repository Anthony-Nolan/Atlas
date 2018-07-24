using System.Linq;
using Nova.SearchAlgorithm.Client.Models.SearchResults;

namespace Nova.SearchAlgorithm.Common.Models.SearchResults
{
    public class MatchAndScoreResult
    {
        public MatchResult MatchResult { get; set; }

        public ScoreResult ScoreResult { get; set; }

        public int PotentialMatchCount => MatchResult.MatchedLoci
            .Select(locus =>
            {
                var scoreDetailsAtLocus = ScoreResult.ScoreDetailsForLocus(locus);
                var matchDetailsAtLocus = MatchResult.MatchDetailsForLocus(locus);
                return scoreDetailsAtLocus.IsPotentialMatch ? matchDetailsAtLocus.MatchCount : 0;
            })
            .Sum();
    }
}