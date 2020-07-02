namespace Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet
{
    public class HaplotypeFrequencySetInput
    {
        public FrequencySetSelectionInput DonorInfo { get; set; }
        public FrequencySetSelectionInput PatientInfo { get; set; }
    }
}