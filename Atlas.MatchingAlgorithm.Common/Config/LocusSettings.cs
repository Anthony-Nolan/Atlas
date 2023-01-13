using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
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
        /// Only loci that have required HLA typing, and are therefore preferable to match on first.
        /// </summary>
        public static HashSet<Locus> RequiredLoci => new[] { Locus.A, Locus.B, Locus.Drb1 }.ToHashSet();

        public static HashSet<Locus> OptionalLoci => MatchingOnlyLoci.Except(RequiredLoci).ToHashSet();
    }
}
