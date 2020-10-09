using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Common.Models.SearchResults;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;

namespace Atlas.MatchPrediction.Test.Verification.Models
{
    internal static class ScoreResultExtensions
    {
        public static LociInfo<LocusScoreDetails> ToLociScoreDetailsInfo(this ScoreResult result)
        {
            return new LociInfo<LocusScoreDetails>(
                result.ScoreDetailsAtLocusA,
                result.ScoreDetailsAtLocusB,
                result.ScoreDetailsAtLocusC,
                result.ScoreDetailsAtLocusDpb1,
                result.ScoreDetailsAtLocusDqb1,
                result.ScoreDetailsAtLocusDrb1
            );
        }
    }
}
