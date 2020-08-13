using Atlas.Common.GeneticData;
using System.Collections.Generic;
using Atlas.MatchPrediction.Config;

namespace Atlas.MatchPrediction.ExternalInterface
{
    /// <summary>
    /// Provides access to pertinent, static information related to match prediction.
    /// </summary>
    public static class MatchPredictionInfo
    {
        public static IEnumerable<Locus> MatchPredictionLoci => LocusSettings.MatchPredictionLoci;
    }
}
