using System;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models.SearchResults;

namespace Nova.SearchAlgorithm.Services.Search.Scoring.Categorisation
{
    public static class MatchCategoriser
    {
        public static MatchCategory CategoriseMatch(ScoreResult scoreResult)
        {
            var overallMatchConfidence = scoreResult.OverallMatchConfidence;

            if (overallMatchConfidence == MatchConfidence.Mismatch)
            {
                // The only way to have an overall confidence of mismatch with no per-position mismatch is for all mismatches to be permissive.
                return scoreResult.AllGrades.Any(g => g == MatchGrade.Mismatch) ? MatchCategory.Mismatch : MatchCategory.PermissiveMismatch;
            }

            return MapConfidenceToCategory(overallMatchConfidence);
        }

        private static MatchCategory MapConfidenceToCategory(MatchConfidence matchConfidence)
        {
            switch (matchConfidence)
            {
                case MatchConfidence.Mismatch:
                    return MatchCategory.Mismatch;
                case MatchConfidence.Potential:
                    return MatchCategory.Potential;
                case MatchConfidence.Exact:
                    return MatchCategory.Exact;
                case MatchConfidence.Definite:
                    return MatchCategory.Definite;
                default:
                    throw new ArgumentOutOfRangeException(nameof(matchConfidence), matchConfidence, null);
            }
        }
    }
}