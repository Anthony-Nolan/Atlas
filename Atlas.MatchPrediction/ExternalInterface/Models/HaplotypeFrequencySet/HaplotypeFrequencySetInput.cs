using Atlas.MatchPrediction.Models;

namespace Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet
{
    public class HaplotypeFrequencySetInput
    {
        public HaplotypeFrequencySetInput(IndividualMetaData donorInfo, IndividualMetaData patientInfo)
        {
            DonorInfo = donorInfo;
            PatientInfo = patientInfo;
        }
        public IndividualMetaData DonorInfo { get; set; }
        public IndividualMetaData PatientInfo { get; set; }
    }
}