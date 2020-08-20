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

        public HaplotypeFrequencySetMetadata(HaplotypeFrequencyFile haplotypeFrequencyFile, string name)
        {
            Registry = haplotypeFrequencyFile.RegistryCode;
            Ethnicity = haplotypeFrequencyFile.EthnicityCode;
            Name = name;
            PopulationId = haplotypeFrequencyFile.PopulationId;
            HlaNomenclatureVersion = haplotypeFrequencyFile.HlaNomenclatureVersion;
        }
    }
}