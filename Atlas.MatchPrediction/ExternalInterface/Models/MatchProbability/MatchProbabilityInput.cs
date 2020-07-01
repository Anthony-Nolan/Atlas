using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Models;

namespace Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability
{
    public class MatchProbabilityInput
    {
        public PhenotypeInfo<string> DonorHla { get; set; }
        public IndividualPopulationData DonorPopulationData { get; set; }
        public PhenotypeInfo<string> PatientHla { get; set; }
        public IndividualPopulationData PatientPopulationData { get; set; }
        public string HlaNomenclatureVersion { get; set; }
    }
}
