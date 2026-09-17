using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies;

/// <summary>
/// Tracks which haplotype frequency set ids are currently resident in <see cref="HaplotypeFrequencyCache"/>'s
/// dedicated cache, in least-recently-used order, and evicts the least-recently-used set to make room whenever a
/// not-yet-tracked set would otherwise push residency over <c>capacity</c>.
///
/// <para>
/// This exists because <c>Microsoft.Extensions.Caching.Memory.MemoryCache</c>'s own <c>SizeLimit</c> does not evict
/// an existing entry to admit a new one once the limit is reached - verified empirically, it rejects the new insert
/// outright instead, so the newest set would simply never get cached rather than displacing the oldest one. Eviction
/// of the correct (least-recently-used) entry has to be driven explicitly, from here, one call before the cache is
/// even asked to store anything.
/// </para>
/// </summary>
internal interface IFrequencySetResidencyTracker
{
    /// <summary>
    /// Records that <paramref name="setId"/> was just accessed: marks it most-recently-used if already tracked, or
    /// admits it - evicting the least-recently-used tracked set first, if at capacity - if not.
    ///
    /// <para>
    /// Called on every <see cref="HaplotypeFrequencyCache.GetAllHaplotypeFrequencies"/> invocation, which is a hot
    /// path (once per surviving pooled haplotype during genotype expansion, potentially millions of times across a
    /// search) - so this has to stay lock-free for the common "already tracked" case.
    /// </para>
    /// </summary>
    void RecordAccess(int setId);

    /// <summary>
    /// Stops tracking <paramref name="setId"/> without evicting anything, i.e. without calling the eviction callback
    /// passed to the constructor. For when the cache entry has already gone away for a reason this tracker didn't
    /// drive itself (TTL expiry, or an explicit removal elsewhere) - keeps residency in sync with what the cache
    /// actually holds, rather than continuing to count a phantom slot against <c>capacity</c>. A no-op if
    /// <paramref name="setId"/> isn't tracked (including because this tracker's own eviction already removed it).
    /// </summary>
    void Forget(int setId);
}

internal sealed class FrequencySetResidencyTracker : IFrequencySetResidencyTracker
{
    private readonly int capacity;
    private readonly Action<int> onEvict;

    // Only taken around the (rare) eviction scan below, never around RecordAccess's common "already tracked" path.
    private readonly object evictionLock = new();

    private readonly ConcurrentDictionary<int, long> lastAccessTickBySetId = new();
    private long tickCounter;

    public FrequencySetResidencyTracker(int capacity, Action<int> onEvict)
    {
        this.capacity = capacity;
        this.onEvict = onEvict;
    }

    public void RecordAccess(int setId)
    {
        var tick = Interlocked.Increment(ref tickCounter);
        lastAccessTickBySetId.AddOrUpdate(setId, tick, (_, _) => tick);

        if (lastAccessTickBySetId.Count <= capacity)
        {
            return;
        }

        EvictDownToCapacity();
    }

    public void Forget(int setId) => lastAccessTickBySetId.TryRemove(setId, out _);

    private void EvictDownToCapacity()
    {
        lock (evictionLock)
        {
            while (lastAccessTickBySetId.Count > capacity)
            {
                var leastRecentlyUsedSetId = FindLeastRecentlyUsedSetId();

                if (leastRecentlyUsedSetId == null || !lastAccessTickBySetId.TryRemove(leastRecentlyUsedSetId.Value, out _))
                {
                    // Someone else already removed it (e.g. via Forget) between the scan and the removal - the
                    // count will reflect that on the next loop check, nothing further to do for this iteration.
                    continue;
                }

                onEvict(leastRecentlyUsedSetId.Value);
            }
        }
    }

    private int? FindLeastRecentlyUsedSetId()
    {
        int? leastRecentlyUsedSetId = null;
        var oldestTick = long.MaxValue;

        foreach (var (setId, tick) in lastAccessTickBySetId)
        {
            if (tick < oldestTick)
            {
                oldestTick = tick;
                leastRecentlyUsedSetId = setId;
            }
        }

        return leastRecentlyUsedSetId;
    }
}
