using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Common.Models.SearchResults;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.Aggregation
{
    public interface IScoreResultAggregator
    {
        AggregateScoreDetails AggregateScoreDetails(ScoreResultAggregatorParameters parameters);
    }

    public class ScoreResultAggregatorParameters
    {
        public ScoreResult ScoreResult { get; set; }
        public IReadOnlyCollection<Locus> ScoredLoci { get; set; }

        /// <summary>
        /// Optional - if empty, results from all scored loci will be included in aggregation.
        /// </summary>
        public IReadOnlyCollection<Locus> LociToExclude { get; set; }
    }

    /// <summary>
    /// TODO ATLAS-544 - confirm how scoring results should be aggregated if only a subset of loci are scored
    /// </summary>
    public class ScoreResultAggregator : IScoreResultAggregator
    {
        public AggregateScoreDetails AggregateScoreDetails(ScoreResultAggregatorParameters parameters)
        {
            var locusScoreDetails = NonExcludedLocusScoreDetails(parameters).ToList();
            
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

        private static IEnumerable<LocusScoreDetails> NonExcludedLocusScoreDetails(ScoreResultAggregatorParameters parameters)
        {
            var includedLoci = parameters.ScoredLoci.Except(parameters.LociToExclude);
            return includedLoci.Select(parameters.ScoreResult.ScoreDetailsForLocus);
        }
    }
}