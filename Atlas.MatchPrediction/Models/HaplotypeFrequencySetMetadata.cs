using Atlas.MatchPrediction.ExternalInterface.Models;

namespace Atlas.MatchPrediction.Models
{
    public class HaplotypeFrequencySetMetadata
    {
        public string Registry { get; set; }
        public string Ethnicity { get; set; }
        public string Name { get; set; }
        public int PopulationId { get; set; }
        public string HlaNomenclatureVersion { get; set; }

        public HaplotypeFrequencySetMetadata()
        {
        }

        public HaplotypeFrequencySetMetadata(HaplotypeFrequency haplotypeFrequency, string name)
        {
            Registry = haplotypeFrequency.RegistryCode;
            Ethnicity = haplotypeFrequency.EthnicityCode;
            Name = name;
            PopulationId = haplotypeFrequency.PopulationId;
            HlaNomenclatureVersion = haplotypeFrequency.HlaNomenclatureVersion;
        }
    }
}