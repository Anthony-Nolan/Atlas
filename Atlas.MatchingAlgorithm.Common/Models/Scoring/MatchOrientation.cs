using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.MatchingAlgorithm.Common.Models.Scoring
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MatchOrientation
    {
        /// <summary>
        /// Locus matches patient position 1 vs donor position 2, and patient position 2 vs donor position 1
        /// </summary>
        Cross,

        /// <summary>
        /// Locus matches patient position 1 vs donor position 1, and patient position 2 vs donor position 2
        /// </summary>
        Direct
    }
}