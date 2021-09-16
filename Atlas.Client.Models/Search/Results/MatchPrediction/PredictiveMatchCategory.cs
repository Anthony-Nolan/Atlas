using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.Client.Models.Search.Results.MatchPrediction
{
    /// <summary>
    /// Values used to categorise the overall match probability.
    /// 
    /// Distinct from MatchCategory and MatchConfidence as they are calculated in the matching algorithm,
    /// whereas this is caclulated from Match Prediction data.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PredictiveMatchCategory
    {
        /// <summary>
        /// Molecular matches within a single p-group.
        /// </summary>
        Exact,

        /// <summary>
        /// Potential match.
        /// </summary>
        Potential,

        /// <summary>
        /// At least one known (non-permissive) mismatch.
        /// </summary>
        Mismatch
    }
}
