using System.Collections.Generic;
using System.Linq;
using Atlas.MatchingAlgorithm.Services.Debug.Precompute;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.MatchingAlgorithm.Functions.DataRefresh.Models.Debug.Precompute;

/// <summary>The parity check of one genotype set: its stored row, as search reads it, compared with the set computed live.</summary>
internal class ParityCheck
{
    /// <summary>
    /// <c>Match</c>, <c>Mismatch</c>, <c>StaleFrequencySet</c>, <c>TypingChanged</c>, <c>NoStoredRow</c>,
    /// <c>NotStoredUnambiguous</c> (no row by design, ATL-478), <c>DecodeFailed</c> or <c>Error</c>. When more than one
    /// applies, the first in the order that search checks a row: <c>Error</c>, <c>NoStoredRow</c> or
    /// <c>NotStoredUnambiguous</c>, <c>TypingChanged</c>, <c>StaleFrequencySet</c>, <c>DecodeFailed</c>, then
    /// <c>Mismatch</c> or <c>Match</c>.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public ParityResult Result { get; set; }

    /// <summary>The typing key of the stored row. Not given when no row is stored.</summary>
    public string StoredHlaTypingKey { get; set; }

    /// <summary>The frequency set of the stored row. Not given when no row is stored.</summary>
    public int? StoredHaplotypeFrequencySetId { get; set; }

    /// <summary>For a donor: the frequency set that search would use now, chosen from the donor's codes.</summary>
    public int? SearchHaplotypeFrequencySetId { get; set; }

    /// <summary>Why the stored data could not be decoded.</summary>
    public string DecodeError { get; set; }

    /// <summary>Why the check could not run: the live computation failed, or the stored row could not be read.</summary>
    public string Error { get; set; }

    /// <summary>The number of all the differences. Not given when the comparison did not run.</summary>
    public int? DifferenceCount { get; set; }

    /// <summary>The first differences, at most <c>MaxDifferencesPerRow</c>, in a fixed order.</summary>
    public List<ParityDifference> Differences { get; set; }

    public static ParityCheck From(GenotypeSetParityCheck check) => check == null
        ? null
        : new ParityCheck
        {
            Result = check.Result,
            StoredHlaTypingKey = check.StoredHlaTypingKey,
            StoredHaplotypeFrequencySetId = check.StoredHaplotypeFrequencySetId,
            SearchHaplotypeFrequencySetId = check.SearchHaplotypeFrequencySetId,
            DecodeError = check.DecodeError,
            Error = check.Error,
            DifferenceCount = check.DifferenceCount,
            Differences = check.Differences.Select(ParityDifference.From).ToList()
        };
}
