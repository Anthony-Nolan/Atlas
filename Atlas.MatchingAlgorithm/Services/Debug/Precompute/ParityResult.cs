namespace Atlas.MatchingAlgorithm.Services.Debug.Precompute;

/// <summary>
/// The result of the parity check of one genotype set: how its stored row compares with the set computed live.
/// </summary>
/// <remarks>
/// When more than one result applies, the check gives the first in the order that search checks a stored row:
/// <see cref="Error"/>, <see cref="NoStoredRow"/> or <see cref="NotStoredUnambiguous"/>, <see cref="StaleFrequencySet"/>,
/// <see cref="DecodeFailed"/>, then <see cref="Mismatch"/> or <see cref="Match"/>.
/// </remarks>
public enum ParityResult
{
    /// <summary>The stored set is the same as the live set.</summary>
    Match,

    /// <summary>The stored set is not the same as the live set. The differences are given.</summary>
    Mismatch,

    /// <summary>
    /// For a donor: the request names a frequency set that search would not use for the donor now, and the row of that
    /// set is read. Search reads the row of its own set instead. The comparison still runs, to show the effect.
    /// </summary>
    StaleFrequencySet,

    /// <summary>
    /// No row has the key of the live set, for a donor as for a typing. The key includes the frequency set, so a row of
    /// an older set is not found. A donor's link rows are not read: search does not read them either (ATL-424).
    /// </summary>
    NoStoredRow,

    /// <summary>
    /// No row is stored, by design: the typing is unambiguous at small g group for the combination, so its one genotype
    /// is computed live without the frequency set, and the pre-computation does not store it (ATL-478).
    /// </summary>
    NotStoredUnambiguous,

    /// <summary>The stored data cannot be decoded, so search computes the subject live.</summary>
    DecodeFailed,

    /// <summary>The set could not be computed live, or its stored row could not be read.</summary>
    Error
}
