using System.Linq;

namespace Nova.SearchAlgorithm.Data.Models.SearchResults
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