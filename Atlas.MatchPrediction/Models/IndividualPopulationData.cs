namespace Atlas.MatchPrediction.Models
{
    /// <summary>
    /// This is the data used to determine which frequency set to use, for both Donors and Patients.
    /// </summary>
    public class IndividualPopulationData
    {
        public string EthnicityId { get; set; }
        public string RegistryId { get; set; }
    }
}