using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;

namespace Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability
{
    public class MatchProbabilityInput
    {
        /// <summary>
        /// Donor ID is not strictly necessary for running match prediction, but will be useful for logging
        /// </summary>
        public int DonorId { get; set; }
        
        /// <summary>
        /// Search ID is not necessary for running match prediction, but will be useful for logging
        /// </summary>
        public string SearchRequestId { get; set; }
        
        public PhenotypeInfo<string> DonorHla { get; set; }
        public FrequencySetMetadata DonorFrequencySetMetadata { get; set; }
        public PhenotypeInfo<string> PatientHla { get; set; }
        public FrequencySetMetadata PatientFrequencySetMetadata { get; set; }
        public string HlaNomenclatureVersion { get; set; }
    }
}
