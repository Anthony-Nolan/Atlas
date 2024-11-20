using Atlas.Client.Models.Common.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.Client.Models.Search.Results.Matching.PerLocus
{
    /// <summary>
    /// This enum represents the same type of data as <see cref="MatchCategory"/>,
    /// but in a per-locus context (aggregated from the locus' data) rather then aggregated over the whole donor.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum LocusMatchCategory
    {
        Match,
        PermissiveMismatch,
        Mismatch,
        Unknown
    }
}