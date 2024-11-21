using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.Client.Models.Common.Results
{
    /// <summary>
    ///     Values used to categorise an overall match.
    ///     Distinct from MatchGrade and MatchConfidence - each of which are calculated on a per-locus/position level.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MatchCategory
    {
        /// <summary>
        ///     Molecular, single-allele resolution matches across all loci.
        /// </summary>
        Definite,

        /// <summary>
        ///     Molecular matches within a single p-group at all loci.
        /// </summary>
        Exact,

        /// <summary>
        ///     Potential match at all loci.
        /// </summary>
        Potential,

        /// <summary>
        ///     Mismatches at DPB1, but all known mismatches are permissive in nature.
        /// </summary>
        PermissiveMismatch,

        /// <summary>
        ///     At least one known (non-permissive) mismatch at any locus.
        /// </summary>
        Mismatch
    }
}