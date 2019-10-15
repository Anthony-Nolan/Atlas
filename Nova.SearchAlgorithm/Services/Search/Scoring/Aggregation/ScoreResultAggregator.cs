using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.SearchResults;

namespace Nova.SearchAlgorithm.Services.Search.Scoring.Aggregation
{
    public interface IScoreResultAggregator
    {
        AggregateScoreDetails AggregateScoreDetails(ScoreResult scoreResult, IReadOnlyCollection<Locus> lociToExclude = null);
    }

    public class ScoreResultAggregator : IScoreResultAggregator
    {
        public AggregateScoreDetails AggregateScoreDetails(ScoreResult scoreResult, IReadOnlyCollection<Locus> lociToExclude = null)
        {
            return new AggregateScoreDetails
            {
                ConfidenceScore = AggregateConfidenceScore(scoreResult, lociToExclude),
                GradeScore = AggregateGradeScore(scoreResult, lociToExclude),
                MatchCategory = CategoriseMatch(scoreResult, lociToExclude),
                MatchCount = CountMatches(scoreResult, lociToExclude),
                OverallMatchConfidence = AggregateMatchConfidence(scoreResult, lociToExclude),
                PotentialMatchCount = CountPotentialMatches(scoreResult, lociToExclude),
            };
        }

        private static int AggregateConfidenceScore(ScoreResult scoreResult, IReadOnlyCollection<Locus> lociToExclude)
        {
            return scoreResult.LocusScoreDetails.Sum(s => s.MatchConfidenceScore);
        }

        private static int AggregateGradeScore(ScoreResult scoreResult, IReadOnlyCollection<Locus> lociToExclude)
        {
            return scoreResult.LocusScoreDetails.Sum(s => s.MatchGradeScore);
        }

        private MatchCategory CategoriseMatch(ScoreResult scoreResult, IReadOnlyCollection<Locus> lociToExclude)
        {
            var overallMatchConfidence = AggregateMatchConfidence(scoreResult, lociToExclude);
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

        private static int CountMatches(ScoreResult scoreResult, IReadOnlyCollection<Locus> lociToExclude)
        {
            return scoreResult.LocusScoreDetails.Sum(s => s.MatchCount());
        }

        private static MatchConfidence AggregateMatchConfidence(ScoreResult scoreResult, IReadOnlyCollection<Locus> lociToExclude)
        {
            return scoreResult.LocusScoreDetails
                .SelectMany(d => new List<MatchConfidence> {d.ScoreDetailsAtPosition1.MatchConfidence, d.ScoreDetailsAtPosition2.MatchConfidence})
                .Min();
        }

        private static int CountPotentialMatches(ScoreResult scoreResult, IReadOnlyCollection<Locus> lociToExclude)
        {
            return scoreResult.LocusScoreDetails.Where(s => s.IsPotentialMatch).Sum(s => s.MatchCount());
        }
    }
}