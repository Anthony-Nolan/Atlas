using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability
{
    public class MatchProbabilityInput
    {
        /// <summary>
        /// Donor ID is not strictly necessary for running match prediction, but will be useful for logging
        /// </summary>
        public int DonorId { get; set; }
        public PhenotypeInfo<string> DonorHla { get; set; }
        public FrequencySetSelectionInput DonorFrequencySetSelectionInput { get; set; }
        public PhenotypeInfo<string> PatientHla { get; set; }
        public FrequencySetSelectionInput PatientFrequencySetSelectionInput { get; set; }
        public string HlaNomenclatureVersion { get; set; }
    }
}
