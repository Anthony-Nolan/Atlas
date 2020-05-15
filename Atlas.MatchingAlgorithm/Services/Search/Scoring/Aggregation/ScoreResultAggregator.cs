using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Atlas.MatchingAlgorithm.Common.Models.SearchResults;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using static EnumStringValues.EnumExtensions;
using Atlas.Utils.Models;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.Aggregation
{
    public interface IScoreResultAggregator
    {
        AggregateScoreDetails AggregateScoreDetails(ScoreResult scoreResult, IReadOnlyCollection<Locus> lociToExclude = null);
    }

    public class ScoreResultAggregator : IScoreResultAggregator
    {
        public AggregateScoreDetails AggregateScoreDetails(ScoreResult scoreResult, IReadOnlyCollection<Locus> lociToExclude)
        {
            lociToExclude = lociToExclude ?? new List<Locus>();
            var locusScoreDetails = NonExcludedLocusScoreDetails(scoreResult, lociToExclude).ToList();
            
            return new AggregateScoreDetails
            {
                ConfidenceScore = AggregateConfidenceScore(locusScoreDetails),
                GradeScore = AggregateGradeScore(locusScoreDetails),
                MatchCategory = CategoriseMatch(locusScoreDetails),
                MatchCount = CountMatches(locusScoreDetails),
                OverallMatchConfidence = AggregateMatchConfidence(locusScoreDetails),
                PotentialMatchCount = CountPotentialMatches(locusScoreDetails),
                TypedLociCount = CountTypedLoci(locusScoreDetails)
            };
        }

        private static int AggregateConfidenceScore(IEnumerable<LocusScoreDetails> locusScoreResults)
        {
            return locusScoreResults.Sum(s => s.MatchConfidenceScore);
        }

        private static int AggregateGradeScore(IEnumerable<LocusScoreDetails> locusScoreResults)
        {
            return locusScoreResults.Sum(s => s.MatchGradeScore);
        }

        private MatchCategory CategoriseMatch(IReadOnlyCollection<LocusScoreDetails> locusScoreResults)
        {
            var overallMatchConfidence = AggregateMatchConfidence(locusScoreResults);
            var allGrades = locusScoreResults.SelectMany(locusScoreDetails => new List<MatchGrade>
            {
                locusScoreDetails.ScoreDetailsAtPosition1.MatchGrade,
                locusScoreDetails.ScoreDetailsAtPosition2.MatchGrade
            });
            
            switch (overallMatchConfidence)
            {
                case MatchConfidence.Mismatch:
                    // The only way to have an overall confidence of mismatch with no per-position mismatch is for all mismatches to be permissive.
                    return allGrades.All(g => g != MatchGrade.Mismatch) ? MatchCategory.PermissiveMismatch : MatchCategory.Mismatch;
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

        private static int CountMatches(IEnumerable<LocusScoreDetails> locusScoreResults)
        {
            return locusScoreResults.Sum(s => s.MatchCount());
        }

        private static MatchConfidence AggregateMatchConfidence(IEnumerable<LocusScoreDetails> locusScoreResults)
        {
            return locusScoreResults
                .SelectMany(d => new List<MatchConfidence> {d.ScoreDetailsAtPosition1.MatchConfidence, d.ScoreDetailsAtPosition2.MatchConfidence})
                .Min();
        }

        private static int CountPotentialMatches(IEnumerable<LocusScoreDetails> locusScoreResults)
        {
            return locusScoreResults.Where(s => s.IsPotentialMatch).Sum(s => s.MatchCount());
        }

        private static int CountTypedLoci(IEnumerable<LocusScoreDetails> locusScoreDetails)
        {
            return locusScoreDetails.Count(m => m.IsLocusTyped);
        }

        private static IEnumerable<LocusScoreDetails> NonExcludedLocusScoreDetails(ScoreResult scoreResult, IEnumerable<Locus> lociToExclude)
        {
            var includedLoci = EnumerateValues<Locus>().Except(lociToExclude);
            return includedLoci.Select(scoreResult.ScoreDetailsForLocus);
        }
    }
}