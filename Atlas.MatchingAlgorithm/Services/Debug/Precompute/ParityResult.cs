namespace Atlas.MatchingAlgorithm.Services.Debug.Precompute;

/// <summary>
/// The result of the parity check of one genotype set: how its stored row compares with the set computed live.
/// </summary>
/// <remarks>
/// When more than one result applies, the check gives the first in the order that search checks a stored row:
/// <see cref="Error"/>, <see cref="NoStoredRow"/>, <see cref="TypingChanged"/>, <see cref="StaleFrequencySet"/>,
/// <see cref="DecodeFailed"/>, then <see cref="Mismatch"/> or <see cref="Match"/>.
/// </remarks>
public enum ParityResult
{
    /// <summary>The stored set is the same as the live set.</summary>
    Match,

    /// <summary>The stored set is not the same as the live set. The differences are given.</summary>
    Mismatch,

    /// <summary>
    /// For a donor: the stored row was computed with a frequency set that search would not use now, so search computes
    /// the donor live. The comparison still runs, to show the effect.
    /// </summary>
    StaleFrequencySet,

    /// <summary>
    /// For a donor: the stored row was computed from a typing that is not the donor's typing now, so search does not use
    /// the row. The comparison still runs.
    /// </summary>
    TypingChanged,

    /// <summary>
    /// No row is stored. For a donor: it has no link row for the combination. For a typing: no row has the key of the
    /// live set, which includes the frequency set, so a row of an older set is not found.
    /// </summary>
    NoStoredRow,

    /// <summary>The stored data cannot be decoded, so search computes the subject live.</summary>
    DecodeFailed,

    /// <summary>The set could not be computed live, or its stored row could not be read.</summary>
    Error
}
