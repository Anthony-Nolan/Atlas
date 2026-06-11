using System.Collections.Frozen;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies;

/// <summary>
/// A single cached unit of work per haplotype frequency set: the raw per-set frequencies, the interner that
/// produced their keys, and - once the background pre-consolidation completes - the consolidated (missing loci)
/// frequencies. Holding all three in one object guarantees they share a single interner and a single cache
/// lifetime, so the consolidated keys can never drift from the set they were derived from.
/// </summary>
public sealed class FrequencySetCacheEntry
{
    public required FrozenDictionary<HaplotypeKey, HaplotypeFrequencyValue> SetFrequencies { get; init; }

    public required HaplotypeInterner Interner { get; init; }

    // Null until the background pre-consolidation completes. Keyed by the same Interner as SetFrequencies, so
    // lookups are guaranteed consistent. Callers fall back to a direct calculation while this is null.
    // volatile: reference writes are already atomic, so a stale read merely takes the (correct) direct path -
    // the barrier just lets readers pick up the populated collection promptly.
    private volatile FrozenDictionary<HaplotypeKey, decimal> consolidatedFrequencies;

    public FrozenDictionary<HaplotypeKey, decimal> ConsolidatedFrequencies
    {
        get => consolidatedFrequencies;
        set => consolidatedFrequencies = value;
    }
}
