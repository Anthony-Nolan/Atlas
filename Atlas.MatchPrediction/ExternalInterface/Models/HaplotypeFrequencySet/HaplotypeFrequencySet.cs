using Atlas.MatchPrediction.Models;

namespace Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet
{
    public class HaplotypeFrequencySet
    {
        public HaplotypeFrequencySet(Data.Models.HaplotypeFrequencySet set)
        {
            Id = set.Id;
            RegistryCode = set.RegistryCode;
            EthnicityCode = set.EthnicityCode;
            Name = set.Name;
        }
        public int Id { get; set; }
        public string RegistryCode { get; set; }
        public string EthnicityCode { get; set; }
        public string Name { get; set; }
    }
}