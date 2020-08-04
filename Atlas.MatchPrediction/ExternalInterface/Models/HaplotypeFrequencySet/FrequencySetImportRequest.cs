namespace Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet
{
    public class FrequencySetImportRequest
    {
        public string FileName { get; set; }
        public string RegistryCode { get; set; }
        public string EthnicityCode { get; set; }

        /// <summary>
        /// Should the imported haplotypes be converted to P groups, where possible?
        /// Defaults to <value>true</value> if not provided.
        /// </summary>
        public bool ConvertToPGroups { get; set; } = true;
    }
}
