using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Client.Models.MatchCalculation
{
    public class MatchCalculationResponse
    {
        public LociInfo<int> MatchCounts { get; set; }
        public bool IsTenOutOfTenMatch => MatchCounts.Reduce((locus, value, accumulator) => accumulator + value, 0) == 10;
    }
}
