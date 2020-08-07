namespace Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability
{
    /// <summary>
    /// Values used to categorise the overall match probability.
    /// Distinct from MatchCategory and MatchConfidence - As this will be part of the search result.
    /// </summary>
    public enum PredictiveMatchCategory
    {
        /// <summary>
        /// Molecular matches within a single p-group at all loci.
        /// </summary>
        Exact,

        /// <summary>
        /// Potential match at all loci.
        /// </summary>
        Potential,

        /// <summary>
        /// At least one known (non-permissive) mismatch at any locus.
        /// </summary>
        Mismatch
    }
}
