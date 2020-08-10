namespace Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability
{
    /// <summary>
    /// Values used to categorise the overall match probability.
    /// 
    /// Distinct from MatchCategory and MatchConfidence as they are calculated in the matching algorithm,
    /// whereas this is caclulated from Match Prediction data.
    /// </summary>
    public enum PredictiveMatchCategory
    {
        /// <summary>
        /// Molecular matches within a single p-group.
        /// </summary>
        Exact,

        /// <summary>
        /// Potential match at given locus.
        /// </summary>
        Potential,

        /// <summary>
        /// At least one known (non-permissive) mismatch at given locus.
        /// </summary>
        Mismatch
    }
}
