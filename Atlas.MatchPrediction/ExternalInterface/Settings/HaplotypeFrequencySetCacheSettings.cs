using System.ComponentModel.DataAnnotations;

namespace Atlas.MatchPrediction.ExternalInterface.Settings
{
    public class HaplotypeFrequencySetCacheSettings
    {
        /// <summary>
        /// How long each process keeps the active haplotype frequency sets, before it reads them again from the distributed
        /// cache (or from the database, when there is no distributed cache). So it is also how long an imported set can take
        /// to reach every process. An absolute expiry, not a sliding one.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int ActiveSetCacheExpiryMinutes { get; set; }

        /// <summary>
        /// The memory, in MiB, that the haplotype frequency sets (<c>FrequencySetCacheEntry</c>) in
        /// <c>HaplotypeFrequencyCache</c>'s dedicated cache can use at once in each process: an estimate from the
        /// number of haplotypes of each set, with its consolidated frequencies. Bounds the process-wide memory this
        /// cache can consume, which is otherwise unbounded: every distinct set (registry/ethnicity) used within
        /// <see cref="SetCacheExpiryMinutes"/> would otherwise stay fully resident. A limit in bytes, not in sets: the
        /// sets differ in size by about a hundred times.
        ///
        /// <para>
        /// Enforced as a true least-recently-used eviction, by <c>FrequencySetResidencyTracker</c>: when a loaded set
        /// takes the total over this limit, the least-recently-used other sets are evicted until it fits, so the fresh
        /// set is always the one kept. A set that is larger than the whole limit stays, alone. This is driven
        /// explicitly rather than left to <c>Microsoft.Extensions.Caching.Memory.MemoryCache</c>'s own
        /// <c>SizeLimit</c>, because that mechanism does not evict on overflow - verified empirically, it rejects the
        /// new insert outright instead, which would starve the newest set of caching rather than displacing the oldest
        /// one.
        /// </para>
        /// </summary>
        [Range(1, int.MaxValue)]
        public int MaxCachedFrequencySetMegabytes { get; set; }

        /// <summary>
        /// Dedicated sliding expiry for a cached haplotype frequency set (<c>FrequencySetCacheEntry</c>): a set that is
        /// not used for this time is removed from the cache of the process. A set in use stays, until
        /// <see cref="MaxCachedFrequencySetMegabytes"/> evicts it.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int SetCacheExpiryMinutes { get; set; }

        /// <summary>
        /// Whether loading a haplotype frequency set should <b>wait</b> for its missing-loci pre-consolidation instead of
        /// racing an unawaited background task. Defaults to <c>false</c>, which is the behaviour every host had before
        /// this setting existed.
        ///
        /// <para>
        /// <b>Leave it false for anything serving a search.</b> The pre-consolidation is three passes over the whole set
        /// - up to 274,606 haplotypes - so awaiting it puts that on the latency of the first search to touch the set
        /// after a cache expiry. Racing it costs a per-haplotype linear scan for callers that arrive during the warm,
        /// which is the right trade when one request is waiting.
        /// </para>
        ///
        /// <para>
        /// <b>Set it true for a precompute</b>, where no single request's latency matters and the same set serves
        /// thousands of donors. Measured, the race is ~9.5% of the blended per-row cost: 83-93 donors of 19,000 -
        /// under half a percent - lose it, and each pays ~1.67 ms per scan against 1.6 µs for a warm read.
        /// </para>
        ///
        /// <para>
        /// Note what this does <i>not</i> change: the pre-consolidation runs either way, and there is exactly one writer
        /// of <c>FrequencySetCacheEntry.ConsolidatedFrequencies</c> in both modes. What moves is whether that work is on
        /// the critical path or beside it. Note also that a machine with spare cores will show this as a wall-clock
        /// regression - the warm no longer overlaps the first donors - while a core-constrained replica should not.
        /// </para>
        /// </summary>
        public bool AwaitConsolidatedFrequencyWarm { get; set; }
    }
}
