using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability
{
    public class MatchProbabilityResponse
    {
        public decimal ZeroMismatchProbability { get; set; }
        public LociInfo<decimal?> MatchProbabilityPerLocus { get; set; }
    }
}
