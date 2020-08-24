using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.ExternalInterface.Models
{
    public class HaplotypeFrequencyMetadata
    {
        public string A { get; set; }
        public string B { get; set; }
        public string C { get; set; }
        public string Dqb1 { get; set; }
        public string Drb1 { get; set; }
        public decimal Frequency { get; set; }
        public string HlaNomenclatureVersion { get; set; }
        public int PopulationId { get; set; }
        public string RegistryCode { get; set; }
        public string EthnicityCode { get; set; }

        public HaplotypeFrequencyMetadata()
        {
        }

        public HaplotypeFrequencyMetadata(LociInfo<string> locusInfo)
        {
            A = locusInfo.A;
            B = locusInfo.B;
            C = locusInfo.C;
            Dqb1 = locusInfo.Dqb1;
            Drb1 = locusInfo.Drb1;
        }
    }
}
