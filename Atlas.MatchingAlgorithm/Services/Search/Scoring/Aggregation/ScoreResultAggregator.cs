using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Common.Models.SearchResults;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Client.Models.Common.Results;

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
            var nonDpb1ScoreDetails = NonExcludedLocusScoreDetailsWithoutDpb1(parameters).ToList();
            var dpb1ScoreDetails = parameters.LociToExclude.Contains(Locus.Dpb1) ? null : parameters.ScoreResult?.ScoreDetailsAtLocusDpb1;

            return new AggregateScoreDetails
            {
                ConfidenceScore = AggregateConfidenceScore(locusScoreDetails),
                GradeScore = AggregateGradeScore(locusScoreDetails),
                MatchCategory = CategoriseMatch(nonDpb1ScoreDetails, dpb1ScoreDetails),
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

        private static MatchCategory CategoriseMatch(IList<LocusScoreDetails> nonDpb1ScoreResults, LocusScoreDetails dpb1ScoreDetails)
        {
            var locusScoreResults = nonDpb1ScoreResults.Concat(new[] { dpb1ScoreDetails }).Where(x => x != null);
            var overallMatchConfidence = AggregateMatchConfidence(locusScoreResults);

            var onlyMismatchIsPermissive = nonDpb1ScoreResults.All(r => r.MatchCategory == LocusMatchCategory.Match) &&
                                           dpb1ScoreDetails?.MatchCategory == LocusMatchCategory.PermissiveMismatch;

            return overallMatchConfidence switch
            {
                MatchConfidence.Mismatch => onlyMismatchIsPermissive ? MatchCategory.PermissiveMismatch : MatchCategory.Mismatch,
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
                .SelectMany(d => new List<MatchConfidence> { d.ScoreDetailsAtPosition1.MatchConfidence, d.ScoreDetailsAtPosition2.MatchConfidence })
                .Min();
        }

        private static int CountPotentialMatches(IEnumerable<LocusScoreDetails> locusScoreResults)
        {
            return locusScoreResults.Sum(s => s.PotentialMatchCount());
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

        private static IEnumerable<LocusScoreDetails> NonExcludedLocusScoreDetailsWithoutDpb1(ScoreResultAggregatorParameters parameters)
        {
            var includedLoci = parameters.ScoredLoci.Except(parameters.LociToExclude).Except(new[] { Locus.Dpb1 });
            return includedLoci.Select(parameters.ScoreResult.ScoreDetailsForLocus);
        }
    }
}