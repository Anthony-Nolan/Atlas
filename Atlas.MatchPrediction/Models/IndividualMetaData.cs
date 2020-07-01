namespace Atlas.MatchPrediction.Models
{
    public class IndividualMetaData
    {
        public IndividualMetaData(string ethnicityId, string registryId)
        { 
            EthnicityId = ethnicityId;
            RegistryId = registryId;
        }
        public string EthnicityId { get; set; }
        public string RegistryId { get; set; }
    }
}