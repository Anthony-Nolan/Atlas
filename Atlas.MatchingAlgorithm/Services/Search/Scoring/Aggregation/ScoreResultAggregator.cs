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
            var dpb1ScoreDetails = parameters.ScoreResult?.ScoreDetailsAtLocusDpb1;

            return new AggregateScoreDetails
            {
                ConfidenceScore = AggregateConfidenceScore(locusScoreDetails),
                GradeScore = AggregateGradeScore(locusScoreDetails),
                MatchCategory = CategoriseMatch(locusScoreDetails, dpb1ScoreDetails),
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

        private static MatchCategory CategoriseMatch(IEnumerable<LocusScoreDetails> locusScoreResults, LocusScoreDetails dpb1ScoreDetails)
        {
            var overallMatchConfidence = AggregateMatchConfidence(locusScoreResults);

            return overallMatchConfidence switch
            {
                MatchConfidence.Mismatch =>
                    dpb1ScoreDetails?.MatchCategory == LocusMatchCategory.PermissiveMismatch
                        ? MatchCategory.PermissiveMismatch
                        : MatchCategory.Mismatch,
                MatchConfidence.Potential => MatchCategory.Potential,
                MatchConfidence.Exact => MatchCategory.Exact,
                MatchConfidence.Definite => MatchCategory.Definite,
                _ => throw new ArgumentOutOfRangeException(nameof(overallMatchConfidence), overallMatchConfidence, null)
            };
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