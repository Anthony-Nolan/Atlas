using Atlas.Common.Caching;
using LazyCache;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies;

/// <summary>
/// A cache dedicated to <see cref="HaplotypeFrequencyCache"/>, rather than the shared, unbounded
/// <see cref="IPersistentCacheProvider"/> also used by the HLA Metadata Dictionary. Isolating it here means the
/// eviction <see cref="FrequencySetResidencyTracker"/> drives against this cache can never touch entries belonging
/// to an unrelated consumer of the shared persistent singleton.
/// </summary>
internal interface IHaplotypeFrequencySetCacheProvider : Atlas.Common.Caching.ICacheProvider
{
}

internal class HaplotypeFrequencySetCacheProvider : IHaplotypeFrequencySetCacheProvider
{
    public IAppCache Cache { get; }

    public HaplotypeFrequencySetCacheProvider(IAppCache cache)
    {
        Cache = cache;
    }
}
