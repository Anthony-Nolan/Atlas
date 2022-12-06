using System.Linq;
using Atlas.Common.GeneticData;

namespace Atlas.MatchingAlgorithm.Data.Models.SearchResults
{
    public class MatchAndScoreResult
    {
        public MatchResult MatchResult { get; set; }

        public ScoreResult ScoreResult { get; set; }

        /// <summary>
        /// Total count of Potential matches at those loci covered by both matching *and* scoring.
        /// TODO - ATLAS-539 - Confirm how potential matches should be calculated
        /// </summary>
        public int PotentialMatchCount => MatchResult.MatchedLoci.Select(GetPotentialMatchCountAtLocus).Sum();

        private int GetPotentialMatchCountAtLocus(Locus locus)
        {
            var matchDetailsAtLocus = MatchResult.MatchDetailsForLocus(locus);

            if (matchDetailsAtLocus == null)
            {
                return 0;
            }

            return ScoreResult?.ScoreDetailsForLocus(locus)?.PotentialMatchCount() ?? 0;
        }
    }
}