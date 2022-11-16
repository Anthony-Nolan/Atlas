using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.Client.Models.Search.Results.MatchPrediction
{
    /// <summary>
    /// Values used to categorise the overall match probability.
    /// 
    /// Distinct from MatchCategory and MatchConfidence as they are calculated in the matching algorithm,
    /// whereas this is calculated from Match Prediction data.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PredictiveMatchCategory
    {
        /// <summary>
        /// 100% likelihood of zero mismatches at this locus or position.
        /// </summary>
        Exact,

        /// <summary>
        /// 1-99% likelihood of zero mismatches at this locus or position.
        /// </summary>
        Potential,

        /// <summary>
        /// 0% likelihood of zero mismatches at this locus or position.
        /// </summary>
        Mismatch
    }
}
