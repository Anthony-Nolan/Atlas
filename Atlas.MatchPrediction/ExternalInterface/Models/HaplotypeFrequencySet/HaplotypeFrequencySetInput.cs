namespace Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet
{
    public class HaplotypeFrequencySetInput
    {
        public FrequencySetMetadata DonorInfo { get; set; }
        public FrequencySetMetadata PatientInfo { get; set; }
    }
}