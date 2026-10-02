namespace Atlas.MatchPrediction.ExternalInterface.Settings
{
    /// <summary>
    /// The kill-switch for reading precomputed donor genotype sets at search time (ATL-221).
    /// </summary>
    /// <remarks>
    /// Match prediction probabilities are clinically sensitive, so this gates the whole precomputed path: the lookup, the
    /// decode, and the write-back of donors computed live. When it is off, every donor runs the complete live pipeline.
    /// A search can override it either way with <c>SearchRequest.UsePrecomputedGenotypeSets</c>.
    /// </remarks>
    public class PrecomputedGenotypeSetSettings
    {
        /// <summary>
        /// Defaults to false, so a host that has not had the setting configured keeps computing every donor live.
        /// </summary>
        public bool UsePrecomputedGenotypeSets { get; set; }
    }
}
