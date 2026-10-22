using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies;

/// <summary>
/// Tracks which haplotype frequency sets are currently resident in <see cref="HaplotypeFrequencyCache"/>'s dedicated
/// cache, with the estimated memory of each and in least-recently-used order. When a loaded set takes the total over the
/// capacity in bytes, it evicts the least-recently-used other sets until the total fits.
///
/// <para>
/// A limit in bytes, not in sets: the sets differ in size by about a hundred times, so a number of sets does not limit
/// the memory of the cache.
/// </para>
///
/// <para>
/// This exists because <c>Microsoft.Extensions.Caching.Memory.MemoryCache</c>'s own <c>SizeLimit</c> does not evict
/// an existing entry to admit a new one once the limit is reached - verified empirically, it rejects the new insert
/// outright instead, so the newest set would simply never get cached rather than displacing the oldest one. Eviction
/// of the correct (least-recently-used) entry has to be driven explicitly, from here.
/// </para>
/// </summary>
internal interface IFrequencySetResidencyTracker
{
    /// <summary>
    /// Records that <paramref name="setId"/> was just accessed: marks it most-recently-used, and starts to track it if it
    /// is not tracked yet. A set counts against the capacity only from <see cref="RecordSize"/>, so this evicts nothing.
    ///
    /// <para>
    /// Called on every <see cref="HaplotypeFrequencyCache.GetAllHaplotypeFrequencies"/> invocation, which is a hot
    /// path (once per surviving pooled haplotype during genotype expansion, potentially millions of times across a
    /// search) - so this has to stay lock-free.
    /// </para>
    /// </summary>
    void RecordAccess(int setId);

    /// <summary>
    /// Records that <paramref name="setId"/> was loaded into the cache, with an estimated memory of
    /// <paramref name="sizeInBytes"/>, and evicts the least-recently-used other sets until the total fits the capacity.
    /// A set that is larger than the whole capacity stays, alone: so it is not loaded again on each access.
    /// </summary>
    /// <returns>
    /// The admission of this entry of the set. Give it to <see cref="Forget"/> when the entry goes, so that a late call
    /// for this entry cannot forget the entry that replaced it.
    /// </returns>
    long RecordSize(int setId, long sizeInBytes);

    /// <summary>
    /// Stops tracking <paramref name="setId"/>, and frees its bytes, without evicting anything, i.e. without calling the
    /// eviction callback passed to the constructor. For when the cache entry has already gone away for a reason this
    /// tracker didn't drive itself (TTL expiry, or an explicit removal elsewhere) - keeps residency in sync with what the
    /// cache actually holds, rather than continuing to count a phantom set against the capacity.
    ///
    /// <para>
    /// A no-op unless <paramref name="admission"/> is the latest admission of the set. The cache calls this from a
    /// post-eviction callback, on the thread pool, and by then the set can be loaded again: the call must then leave the
    /// new entry tracked. Also a no-op if this tracker's own eviction already removed the set.
    /// </para>
    /// </summary>
    void Forget(int setId, long admission);
}

internal sealed class FrequencySetResidencyTracker : IFrequencySetResidencyTracker
{
    private readonly long capacityInBytes;
    private readonly Action<int> onEvict;
    private readonly ILogger<FrequencySetResidencyTracker> logger;

    // Taken by RecordSize and Forget, never by RecordAccess. It guards the sizes, the admissions and the total.
    private readonly object sizeLock = new();

    private readonly ConcurrentDictionary<int, TrackedSet> trackedSets = new();
    private long tickCounter;
    private long admissionCounter;
    private long totalSizeInBytes;

    public FrequencySetResidencyTracker(long capacityInBytes, Action<int> onEvict, ILogger<FrequencySetResidencyTracker> logger)
    {
        this.capacityInBytes = capacityInBytes;
        this.onEvict = onEvict;
        this.logger = logger;
    }

    public void RecordAccess(int setId)
    {
        var tick = Interlocked.Increment(ref tickCounter);
        trackedSets.GetOrAdd(setId, static _ => new TrackedSet()).MarkAccessed(tick);
    }

    public long RecordSize(int setId, long sizeInBytes)
    {
        lock (sizeLock)
        {
            // GetOrAdd, not a lookup: Forget can have removed the set while it loaded.
            var trackedSet = trackedSets.GetOrAdd(setId, static _ => new TrackedSet());
            totalSizeInBytes += sizeInBytes - trackedSet.SizeInBytes;
            trackedSet.SizeInBytes = sizeInBytes;
            trackedSet.Admission = ++admissionCounter;

            // Its callers have waited for the load, and use it now: it must not be the next set to go.
            trackedSet.MarkAccessed(Interlocked.Increment(ref tickCounter));

            EvictLeastRecentlyUsedSetsOtherThan(setId);

            if (totalSizeInBytes > capacityInBytes)
            {
                logger.LogWarning(
                    "Haplotype frequency set {SetId} needs an estimated {SetSizeInBytes} bytes, more than the whole capacity of the frequency set cache: {CapacityInBytes} bytes. It stays in the cache alone. Increase HaplotypeFrequencySetCache:MaxCachedFrequencySetMegabytes.",
                    setId,
                    sizeInBytes,
                    capacityInBytes);
            }

            return trackedSet.Admission;
        }
    }

    public void Forget(int setId, long admission)
    {
        lock (sizeLock)
        {
            if (trackedSets.TryGetValue(setId, out var trackedSet)
                && trackedSet.Admission == admission
                && trackedSets.TryRemove(setId, out _))
            {
                totalSizeInBytes -= trackedSet.SizeInBytes;
            }
        }
    }

    private void EvictLeastRecentlyUsedSetsOtherThan(int recordedSetId)
    {
        while (totalSizeInBytes > capacityInBytes)
        {
            var leastRecentlyUsedSetId = FindLeastRecentlyUsedLoadedSetOtherThan(recordedSetId);

            if (leastRecentlyUsedSetId == null)
            {
                // Only the recorded set holds bytes.
                return;
            }

            if (trackedSets.TryRemove(leastRecentlyUsedSetId.Value, out var evictedSet))
            {
                totalSizeInBytes -= evictedSet.SizeInBytes;
                onEvict(leastRecentlyUsedSetId.Value);
            }
        }
    }

    /// <summary>
    /// Only a set with a recorded size: a set that is still loading holds no bytes yet, and its removal from the cache
    /// would only make its callers load it again.
    /// </summary>
    private int? FindLeastRecentlyUsedLoadedSetOtherThan(int recordedSetId)
    {
        int? leastRecentlyUsedSetId = null;
        var oldestTick = long.MaxValue;

        foreach (var (setId, trackedSet) in trackedSets)
        {
            var tick = trackedSet.LastAccessTick;
            if (setId != recordedSetId && trackedSet.SizeInBytes > 0 && tick < oldestTick)
            {
                oldestTick = tick;
                leastRecentlyUsedSetId = setId;
            }
        }

        return leastRecentlyUsedSetId;
    }

    private sealed class TrackedSet
    {
        private long lastAccessTick;

        /// <summary>Written without the lock, by <see cref="RecordAccess"/>.</summary>
        public long LastAccessTick => Volatile.Read(ref lastAccessTick);

        /// <summary>Under the lock only. Zero until the set is loaded.</summary>
        public long SizeInBytes { get; set; }

        /// <summary>Under the lock only. Zero until the set is loaded: no call to <see cref="Forget"/> has it.</summary>
        public long Admission { get; set; }

        public void MarkAccessed(long tick) => Volatile.Write(ref lastAccessTick, tick);
    }
}
