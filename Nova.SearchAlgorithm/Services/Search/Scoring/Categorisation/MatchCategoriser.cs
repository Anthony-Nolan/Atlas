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
            switch (overallMatchConfidence)
            {
                case MatchConfidence.Mismatch:
                    // The only way to have an overall confidence of mismatch with no per-position mismatch is for all mismatches to be permissive.
                    return scoreResult.AllGrades.All(g => g != MatchGrade.Mismatch) ? MatchCategory.PermissiveMismatch : MatchCategory.Mismatch;
                case MatchConfidence.Potential:
                    return MatchCategory.Potential;
                case MatchConfidence.Exact:
                    return MatchCategory.Exact;
                case MatchConfidence.Definite:
                    return MatchCategory.Definite;
                default:
                    throw new ArgumentOutOfRangeException(nameof(overallMatchConfidence), overallMatchConfidence, null);
            }
        }
    }
}