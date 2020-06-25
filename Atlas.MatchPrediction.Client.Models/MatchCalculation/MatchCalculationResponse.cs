using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Client.Models.MatchCalculation
{
    public class MatchCalculationResponse
    {
        public LociInfo<int?> MatchCounts { get; set; }
        public bool IsTenOutOfTenMatch { get; set; }
    }
}
