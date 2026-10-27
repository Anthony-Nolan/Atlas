using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies;

/// <summary>
/// Tracks which haplotype frequency entries are currently resident in <see cref="HaplotypeFrequencyCache"/>'s dedicated
/// cache, with the estimated memory of each, and in least-recently-used order. When a loaded entry takes the total over the
/// capacity in bytes, it evicts the least-recently-used other entries until the total fits.
///
/// <para>
/// An entry is the frequencies of one content, which all the sets with its content hash share, or of one set with no
/// content hash. Each entry has the key of its local cache entry (<see cref="FrequencySetSource.LocalKey"/>): a set that a
/// restore or a copy of the database replaced gets another key, so it needs no eviction here.
/// </para>
///
/// <para>
/// A limit in bytes, not in entries: the sets differ in size by about a hundred times, so a number of entries does not
/// limit the memory of the cache.
/// </para>
///
/// <para>
/// This exists because <c>Microsoft.Extensions.Caching.Memory.MemoryCache</c>'s own <c>SizeLimit</c> does not evict
/// an existing entry to admit a new one once the limit is reached - verified empirically, it rejects the new insert
/// outright instead, so the newest entry would simply never get cached rather than displacing the oldest one. Eviction
/// of the correct (least-recently-used) entry has to be driven explicitly, from here.
/// </para>
/// </summary>
internal interface IFrequencySetResidencyTracker
{
    /// <summary>
    /// Records that the entry with <paramref name="key"/> was just accessed: marks it most-recently-used, and starts to track
    /// it if it is not tracked yet. An entry counts against the capacity only from <see cref="RecordSize"/>, so this evicts
    /// nothing.
    ///
    /// <para>
    /// Called on every <see cref="HaplotypeFrequencyCache.GetAllHaplotypeFrequencies"/> invocation, which is a hot
    /// path (once per surviving pooled haplotype during genotype expansion, potentially millions of times across a
    /// search) - so this has to stay lock-free.
    /// </para>
    /// </summary>
    void RecordAccess(string key);

    /// <summary>
    /// Records that the entry with <paramref name="key"/> starts to load. Its earlier entry, if it had one, has left the
    /// cache, but the <see cref="Forget"/> of that entry can still wait on the thread pool. So this frees the bytes of the
    /// earlier entry now: until <see cref="RecordSize"/>, the key holds no bytes, so it is not an eviction candidate while it
    /// loads, and the late <see cref="Forget"/> finds another admission and does nothing.
    /// </summary>
    void RecordLoadStart(string key);

    /// <summary>
    /// Records that the entry with <paramref name="key"/> was loaded into the cache, with an estimated memory of
    /// <paramref name="sizeInBytes"/>, and evicts the least-recently-used other entries until the total fits the capacity.
    /// An entry that is larger than the whole capacity stays, alone: so it is not loaded again on each access.
    /// </summary>
    /// <returns>
    /// The admission of this entry. Give it to <see cref="Forget"/> when the entry goes, so that a late call for this entry
    /// cannot forget the entry that replaced it.
    /// </returns>
    long RecordSize(string key, long sizeInBytes);

    /// <summary>
    /// Stops tracking the entry with <paramref name="key"/>, and frees its bytes, without evicting anything, i.e. without
    /// calling the eviction callback passed to the constructor. For when the cache entry has already gone away for a reason
    /// this tracker didn't drive itself (TTL expiry, or an explicit removal elsewhere) - keeps residency in sync with what
    /// the cache actually holds, rather than continuing to count a phantom entry against the capacity.
    ///
    /// <para>
    /// A no-op unless <paramref name="admission"/> is the latest admission of the key. The cache calls this from a
    /// post-eviction callback, on the thread pool, and by then the key can be loaded again: the call must then leave the
    /// new entry tracked. Also a no-op if this tracker's own eviction already removed the key.
    /// </para>
    /// </summary>
    void Forget(string key, long admission);
}

internal sealed class FrequencySetResidencyTracker : IFrequencySetResidencyTracker
{
    private readonly long capacityInBytes;
    private readonly Action<string> onEvict;
    private readonly ILogger<FrequencySetResidencyTracker> logger;

    // Taken by all but RecordAccess. It guards the sizes, the admissions and the total.
    private readonly object sizeLock = new();

    private readonly ConcurrentDictionary<string, TrackedEntry> trackedEntries = new(StringComparer.Ordinal);
    private long tickCounter;
    private long admissionCounter;
    private long totalSizeInBytes;

    /// <param name="capacityInBytes">The capacity, in bytes.</param>
    /// <param name="onEvict">Removes the cache entry with the given key.</param>
    /// <param name="logger">The logger.</param>
    public FrequencySetResidencyTracker(long capacityInBytes, Action<string> onEvict, ILogger<FrequencySetResidencyTracker> logger)
    {
        this.capacityInBytes = capacityInBytes;
        this.onEvict = onEvict;
        this.logger = logger;
    }

    public void RecordAccess(string key)
    {
        var tick = Interlocked.Increment(ref tickCounter);
        trackedEntries.GetOrAdd(key, static _ => new TrackedEntry()).MarkAccessed(tick);
    }

    public void RecordLoadStart(string key)
    {
        lock (sizeLock)
        {
            if (trackedEntries.TryGetValue(key, out var trackedEntry))
            {
                totalSizeInBytes -= trackedEntry.SizeInBytes;
                trackedEntry.SizeInBytes = 0;
                trackedEntry.Admission = 0;
            }
        }
    }

    public long RecordSize(string key, long sizeInBytes)
    {
        lock (sizeLock)
        {
            // GetOrAdd, not a lookup: Forget can have removed the key while it loaded.
            var trackedEntry = trackedEntries.GetOrAdd(key, static _ => new TrackedEntry());
            totalSizeInBytes += sizeInBytes - trackedEntry.SizeInBytes;
            trackedEntry.SizeInBytes = sizeInBytes;
            trackedEntry.Admission = ++admissionCounter;

            // Its callers have waited for the load, and use it now: it must not be the next entry to go.
            trackedEntry.MarkAccessed(Interlocked.Increment(ref tickCounter));

            EvictLeastRecentlyUsedEntriesOtherThan(key);

            if (totalSizeInBytes > capacityInBytes)
            {
                logger.LogWarning(
                    "Haplotype frequency entry {EntryKey} needs an estimated {EntrySizeInBytes} bytes, more than the whole capacity of the frequency set cache: {CapacityInBytes} bytes. It stays in the cache alone. Increase HaplotypeFrequencySetCache:MaxCachedFrequencySetMegabytes.",
                    key,
                    sizeInBytes,
                    capacityInBytes);
            }

            return trackedEntry.Admission;
        }
    }

    public void Forget(string key, long admission)
    {
        lock (sizeLock)
        {
            if (trackedEntries.TryGetValue(key, out var trackedEntry)
                && trackedEntry.Admission == admission
                && trackedEntries.TryRemove(key, out _))
            {
                totalSizeInBytes -= trackedEntry.SizeInBytes;
            }
        }
    }

    private void EvictLeastRecentlyUsedEntriesOtherThan(string recordedKey)
    {
        while (totalSizeInBytes > capacityInBytes)
        {
            var leastRecentlyUsedKey = FindLeastRecentlyUsedLoadedEntryOtherThan(recordedKey);

            if (leastRecentlyUsedKey == null)
            {
                // Only the recorded entry holds bytes.
                return;
            }

            if (trackedEntries.TryRemove(leastRecentlyUsedKey, out var evictedEntry))
            {
                totalSizeInBytes -= evictedEntry.SizeInBytes;
                onEvict(leastRecentlyUsedKey);
            }
        }
    }

    /// <summary>
    /// Only an entry with a recorded size: an entry that is still loading holds no bytes yet, and its removal from the
    /// cache would only make its callers load it again.
    /// </summary>
    private string FindLeastRecentlyUsedLoadedEntryOtherThan(string recordedKey)
    {
        string leastRecentlyUsedKey = null;
        var oldestTick = long.MaxValue;

        foreach (var (key, trackedEntry) in trackedEntries)
        {
            var tick = trackedEntry.LastAccessTick;
            if (key != recordedKey && trackedEntry.SizeInBytes > 0 && tick < oldestTick)
            {
                oldestTick = tick;
                leastRecentlyUsedKey = key;
            }
        }

        return leastRecentlyUsedKey;
    }

    private sealed class TrackedEntry
    {
        private long lastAccessTick;

        /// <summary>Written without the lock, by <see cref="RecordAccess"/>.</summary>
        public long LastAccessTick => Volatile.Read(ref lastAccessTick);

        /// <summary>Under the lock only. Zero until the entry is loaded.</summary>
        public long SizeInBytes { get; set; }

        /// <summary>Under the lock only. Zero until the entry is loaded: no call to <see cref="Forget"/> has it.</summary>
        public long Admission { get; set; }

        public void MarkAccessed(long tick) => Volatile.Write(ref lastAccessTick, tick);
    }
}
