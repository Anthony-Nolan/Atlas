using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Common.Config
{
    /// <summary>
    /// Central location from which locus-based functionality can be controlled.
    /// </summary>
    public static class LocusSettings
    {
        private static readonly List<Locus> AllSearchLoci = Enum.GetValues(typeof(Locus)).Cast<Locus>().ToList();

        /// <summary>
        /// All loci considered by the search algorithm, during both matching and scoring.
        /// </summary>
        public static IEnumerable<Locus> AllLoci => AllSearchLoci;

        /// <summary>
        /// Loci that are only considered during matching.
        /// </summary>
        public static IEnumerable<Locus> MatchingOnlyLoci => AllSearchLoci.Except(new[] {Locus.Dpb1});

        /// <summary>
        /// Only loci that are possible to match in Phase I of Matching.
        /// </summary>
        public static IEnumerable<Locus> LociPossibleToMatchInMatchingPhaseOne => new[] { Locus.A, Locus.B, Locus.Drb1 };
    }
}
