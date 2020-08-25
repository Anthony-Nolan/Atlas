namespace Atlas.MatchPrediction.Models
{
    internal class HaplotypeFrequencyFileRecord
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
    }
}
