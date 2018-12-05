using System;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.Common.Models
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
        /// Loci that are only considered during Phase I of matching.
        /// </summary>
        public static IEnumerable<Locus> MatchingPhaseIOnlyLoci => new[] { Locus.A, Locus.B, Locus.Drb1 };
    }
}
