using System.ComponentModel.DataAnnotations;

namespace Atlas.MatchPrediction.ExternalInterface.Settings
{
    public class HaplotypeFrequencySetCacheSettings
    {
        [Range(1, int.MaxValue)]
        public int ActiveSetCacheExpiryMinutes { get; set; }

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
