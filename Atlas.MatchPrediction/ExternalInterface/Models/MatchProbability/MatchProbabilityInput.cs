using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Models;

namespace Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability
{
    public class MatchProbabilityInput
    {
        /// <summary>
        /// Donor ID is not strictly necessary for running match prediction, but will be useful for logging
        /// </summary>
        public int DonorId { get; set; }
        public PhenotypeInfo<string> DonorHla { get; set; }
        public IndividualPopulationData DonorPopulationData { get; set; }
        public PhenotypeInfo<string> PatientHla { get; set; }
        public IndividualPopulationData PatientPopulationData { get; set; }
        public string HlaNomenclatureVersion { get; set; }
    }
}
