using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.ExternalInterface.Models.MatchPredictionSteps.MatchCalculation
{
    public class MatchCalculationResponse
    {
        public LociInfo<int?> MatchCounts { get; set; }
        public bool IsTenOutOfTenMatch { get; set; }
    }
}
