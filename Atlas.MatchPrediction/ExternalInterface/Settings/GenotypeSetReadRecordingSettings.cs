namespace Atlas.MatchPrediction.ExternalInterface.Settings
{
    /// <summary>
    /// Recording of the donor inputs each search batch would read precomputed genotype sets for (ATL-571). The record
    /// is the input of the cache hit-rate measurement in ATL-347. It is a temporary measurement: switch it on only for
    /// the measurement window.
    /// </summary>
    public class GenotypeSetReadRecordingSettings
    {
        /// <summary>Off by default. When off, nothing is recorded and the search path does not change.</summary>
        public bool Enabled { get; set; }

        /// <summary>The blob container the records are written to, in the match prediction results storage account.</summary>
        public string BlobContainer { get; set; } = "genotype-set-read-records";
    }
}
