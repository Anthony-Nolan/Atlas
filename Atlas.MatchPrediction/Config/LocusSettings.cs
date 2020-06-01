using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using static EnumStringValues.EnumExtensions;

namespace Atlas.MatchPrediction.Config
{
    /// <summary>
    /// Central location from which locus-based functionality can be controlled.
    /// </summary>
    public static class LocusSettings
    {
        /// <summary>
        /// Only these loci are considered during match prediction.
        /// </summary>
        public static IEnumerable<Locus> MatchPredictionLoci => EnumerateValues<Locus>().Except(new[] {Locus.Dpb1});
    }
}
