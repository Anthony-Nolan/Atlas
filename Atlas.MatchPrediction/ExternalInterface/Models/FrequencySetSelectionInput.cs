namespace Atlas.MatchPrediction.ExternalInterface.Models
{
    /// <summary>
    /// This is the data used to determine which frequency set to use, for both Donors and Patients.
    /// </summary>
    public class FrequencySetSelectionInput
    {
        public string EthnicityCode { get; set; }
        public string RegistryCode { get; set; }
    }
}