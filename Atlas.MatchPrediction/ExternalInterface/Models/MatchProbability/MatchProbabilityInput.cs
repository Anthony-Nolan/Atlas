using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability
{
    public class MatchProbabilityInput
    {
        public PhenotypeInfo<string> DonorHla { get; set; }
        public PhenotypeInfo<string> PatientHla { get; set; }
        public string HlaNomenclatureVersion { get; set; }
    }
}
