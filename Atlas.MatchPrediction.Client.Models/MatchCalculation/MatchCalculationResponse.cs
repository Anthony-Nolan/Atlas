using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Client.Models.MatchCalculation
{
    public class MatchCalculationResponse
    {
        public LociInfo<string> MatchHla { get; set; }
        public bool IsTenOutOfTenMatch { get; set; }
    }
}
