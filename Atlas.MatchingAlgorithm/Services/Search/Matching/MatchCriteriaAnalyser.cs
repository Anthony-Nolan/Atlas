using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Common.Config;
using Atlas.MatchingAlgorithm.Common.Models;

namespace Atlas.MatchingAlgorithm.Services.Search.Matching
{
    public interface IMatchCriteriaAnalyser
    {
        /// <summary>
        /// Determines the order in which the hla tables are queried.
        ///
        /// Required loci must be queried before optional ones, as untyped donors will hugely inflate the result set size of optional loci if queried first.
        /// Otherwise, attempts to optimise based on a combination of the mismatchCount at each locus, and known properties of that locus (e.g. A is generally a
        /// more ambiguous locus with a larger search space, so tends to be more efficient to query after B/DRB1. 
        /// </summary>
        IList<Locus> LociInMatchingOrder(AlleleLevelMatchCriteria criteria);
    }

    public class MatchCriteriaAnalyser : IMatchCriteriaAnalyser
    {
        private readonly Dictionary<Locus, int> locusPriority = new Dictionary<Locus, int>
        {
            {Locus.Drb1, 1},
            {Locus.B, 2},
            // Prefer to avoid searching for Locus A first as it is the largest dataset, and takes longest to query
            {Locus.A, 3},
            {Locus.Dqb1, 4},
            {Locus.C, 4},
        };

        // TODO: ATLAS-847: Dynamically decide which loci to initially query for based on criteria, optimising for search speed
        public IList<Locus> LociInMatchingOrder(AlleleLevelMatchCriteria criteria)
        {
            var locusMismatchCounts = criteria.LocusCriteria.Map((locus, c) => (locus, c?.MismatchCount))
                .ToEnumerable()
                .Where(x => x.Item2 != null)
                .ToList();

            var requiredLoci = locusMismatchCounts.Where(locusMismatch => LocusSettings.RequiredLoci.Contains(locusMismatch.locus)).ToList();

            var optionalLoci = locusMismatchCounts.Except(requiredLoci);

            var orderedRequiredLoci = requiredLoci
                .OrderBy(x => x.MismatchCount)
                .ThenBy(x => locusPriority[x.locus]);

            return orderedRequiredLoci.Select(x => x.locus).Concat(optionalLoci.Select(x => x.locus)).ToList();
        }
    }
}