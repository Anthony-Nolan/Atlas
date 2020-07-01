using Atlas.MatchPrediction.Models;

namespace Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet
{
    public class HaplotypeFrequencySetInput
    {
        public IndividualPopulationData DonorInfo { get; set; }
        public IndividualPopulationData PatientInfo { get; set; }
    }
}