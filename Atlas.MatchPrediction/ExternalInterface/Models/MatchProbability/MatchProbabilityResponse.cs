using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability
{
    public class MatchProbabilityResponse
    {
        public decimal ZeroMismatchProbability { get; set; }
        public decimal OneMismatchProbability { get; set; }
        public decimal TwoMismatchProbability { get; set; }
        public LociInfo<decimal?> ZeroMismatchProbabilityPerLocus { get; set; }
    }
}
