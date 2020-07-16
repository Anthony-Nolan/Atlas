using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using static EnumStringValues.EnumExtensions;

namespace Atlas.MatchingAlgorithm.Common.Config
{
    /// <summary>
    /// Central location from which locus-based functionality can be controlled.
    /// </summary>
    public static class LocusSettings
    {
        /// <summary>
        /// Loci that are only considered during matching.
        /// </summary>
        public static HashSet<Locus> MatchingOnlyLoci => EnumerateValues<Locus>().Except(new[] {Locus.Dpb1}).ToHashSet();

        /// <summary>
        /// Only loci that are possible to match in Phase I of Matching.
        /// </summary>
        public static HashSet<Locus> LociPossibleToMatchInMatchingPhaseOne => new[] { Locus.A, Locus.B, Locus.Drb1 }.ToHashSet();
    }
}
