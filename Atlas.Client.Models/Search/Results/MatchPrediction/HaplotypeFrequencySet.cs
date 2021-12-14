namespace Atlas.Client.Models.Search.Results.MatchPrediction
{
    public class HaplotypeFrequencySet
    {
        public int Id { get; set; }
        public string RegistryCode { get; set; }
        public string EthnicityCode { get; set; }
        /// <summary>
        /// Nomenclature version used for HF set generation + processing for the donor.
        /// NOTE: HLA expansion of the request was done using the input version.
        /// </summary>
        public string HlaNomenclatureVersion { get; set; }
        public int PopulationId { get; set; }
    }
}