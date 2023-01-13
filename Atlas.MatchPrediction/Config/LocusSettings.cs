using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using static EnumStringValues.EnumExtensions;

namespace Atlas.MatchPrediction.Config
{
    /// <summary>
    /// Central location from which locus-based functionality can be controlled.
    /// </summary>
    internal static class LocusSettings
    {
        /// <summary>
        /// Only these loci are considered during match prediction.
        /// </summary>
        public static ISet<Locus> MatchPredictionLoci => EnumerateValues<Locus>().Except(new[] {Locus.Dpb1}).ToHashSet();
    }
}
