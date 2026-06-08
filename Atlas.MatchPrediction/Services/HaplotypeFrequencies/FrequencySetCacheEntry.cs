using System.Collections.Frozen;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies;

public record FrequencySetCacheEntry<T>(
    FrozenDictionary<HaplotypeKey, T> Frequencies,
    HaplotypeInterner Interner
);