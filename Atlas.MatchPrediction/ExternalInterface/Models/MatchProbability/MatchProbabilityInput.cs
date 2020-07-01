using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Models;

namespace Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability
{
    public class MatchProbabilityInput
    {
        public PhenotypeInfo<string> DonorHla { get; set; }
        public IndividualMetaData DonorMetaData { get; set; }
        public PhenotypeInfo<string> PatientHla { get; set; }
        public IndividualMetaData PatientMetaData { get; set; }
        public string HlaNomenclatureVersion { get; set; }
    }
}
