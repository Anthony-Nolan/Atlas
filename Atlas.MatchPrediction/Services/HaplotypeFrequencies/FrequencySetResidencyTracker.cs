using System;
using System.Collections.Generic;

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
    /// </summary>
    void RecordAccess(int setId);
}

internal sealed class FrequencySetResidencyTracker : IFrequencySetResidencyTracker
{
    private readonly int capacity;
    private readonly Action<int> onEvict;
    private readonly object lockObject = new();

    // Front = least recently used, back = most recently used.
    private readonly LinkedList<int> setIdsByRecency = new();
    private readonly Dictionary<int, LinkedListNode<int>> nodesBySetId = new();

    public FrequencySetResidencyTracker(int capacity, Action<int> onEvict)
    {
        this.capacity = capacity;
        this.onEvict = onEvict;
    }

    public void RecordAccess(int setId)
    {
        lock (lockObject)
        {
            if (nodesBySetId.TryGetValue(setId, out var existingNode))
            {
                setIdsByRecency.Remove(existingNode);
                setIdsByRecency.AddLast(existingNode);
                return;
            }

            while (nodesBySetId.Count >= capacity)
            {
                var leastRecentlyUsed = setIdsByRecency.First!;
                setIdsByRecency.RemoveFirst();
                nodesBySetId.Remove(leastRecentlyUsed.Value);
                onEvict(leastRecentlyUsed.Value);
            }

            nodesBySetId[setId] = setIdsByRecency.AddLast(setId);
        }
    }
}
