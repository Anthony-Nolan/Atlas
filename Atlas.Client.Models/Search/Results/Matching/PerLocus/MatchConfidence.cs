using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.Client.Models.Search.Results.Matching.PerLocus
{
    /// <summary>
    ///     Values for the confidence of a given match. Ordered to allow for selecting the best confidence from a list
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MatchConfidence
    {
        Mismatch = 0,
        Potential = 1,
        Exact = 2,
        Definite = 3
    }
}