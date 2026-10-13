using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Services.CompressedPhenotypeExpansion;

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

    // In production this is built eagerly by HaplotypeFrequencyCache.BuildEntryFromDatabase, before the entry escapes
    // its cache.GetOrAddAsync, so nothing races the first access. That is where the guarantee lives - the volatile and
    // the ??= below only keep this correct for any other caller, such as a test constructing an entry directly.
    // volatile: a reference write has release semantics, so the collection is fully built before any thread can
    // observe it. ??= rather than a lock: the read-test-write is not atomic, so two threads racing the first access
    // can both project - but a FrozenDictionary enumerates in a stable order, so their results are element-for-element
    // identical and either may win.
    private volatile DataByResolution<HaplotypeKey[]> projectedPool;

    /// <summary>
    /// The set's haplotypes as interned keys, grouped by typing category - the form phenotype expansion filters
    /// against.
    ///
    /// <para>
    /// This projection is a pure function of <see cref="SetFrequencies"/> and <see cref="Interner"/>:
    /// <c>CompressedPhenotypeExpander</c> asks for it by set id and nothing else, not even the allowed loci. It
    /// therefore belongs to the set rather than to a donor, and is memoised here so that only the first donor to touch
    /// a set pays for walking its frequencies.
    /// </para>
    ///
    /// <para>
    /// It lives on the cache entry because the entry already owns both inputs and already has the per-set lifetime
    /// this needs: one <c>IAppCache</c> entry per set id, so the pool cannot outlive, or drift from, the frequencies
    /// it was derived from.
    /// </para>
    ///
    /// <para>
    /// <b>The pool is the set's own interned keys, not their name form.</b> A <see cref="HaplotypeKey"/> is 20 bytes
    /// in a flat array, against roughly 80 for the equivalent <c>LociInfo&lt;string&gt;</c>, and building it calls no
    /// <c>ReverseLookup</c> at all: the keys are already what the frozen dictionary holds. The filter then compares
    /// ids instead of hashing allele names, and only the survivors are resolved back to names.
    /// </para>
    ///
    /// <para>
    /// <b>The ids never leave this object.</b> They are meaningful only against <see cref="Interner"/>, and a second
    /// entry for the same set - built after eviction or expiry - has a different id space. That is safe here because
    /// the pool, the frequencies and the interner are fields of one immutable entry, exactly as the class remark above
    /// states; it is <i>not</i> safe to pass an id to anything that re-enters the cache, such as
    /// <c>HaplotypeFrequencyService.GetFrequencyForHla</c>, which fetches the entry again and may hold a different
    /// instance. Survivors are therefore carried onwards as names.
    /// </para>
    /// </summary>
    internal DataByResolution<HaplotypeKey[]> ProjectedPool => projectedPool ??= ProjectPool();

    /// <summary>
    /// Groups the frozen dictionary's own keys by typing category, preserving its enumeration order.
    ///
    /// <para>
    /// It is tempting to build this straight from the SQL rows in <c>BuildEntryFromDatabase</c>'s existing loop, which
    /// would skip this pass altogether. Do not. <b>The pool's order sets the survivor order, which sets the pairing
    /// order, which sets the genotype set's insertion order - and insertion order is the tie-break
    /// <c>ExpandedGenotypeTruncater.MostLikelyFirst</c> applies when likelihoods are equal.</b> Which genotypes a
    /// capped donor keeps is a clinical output, so a different enumeration order here is a clinical change needing
    /// HLA-expert sign-off, not a performance one.
    /// </para>
    /// </summary>
    private DataByResolution<HaplotypeKey[]> ProjectPool()
    {
        var groupedFrequencies = SetFrequencies
            .GroupBy(f => f.Value.TypingCategory)
            .ToDictionary(
                key => key.Key,
                value => value.Select(f => f.Key).ToArray()
            );

        return new DataByResolution<HaplotypeKey[]>
        {
            GGroup = groupedFrequencies.GetValueOrDefault(HaplotypeTypingCategory.GGroup, []),
            PGroup = groupedFrequencies.GetValueOrDefault(HaplotypeTypingCategory.PGroup, []),
            SmallGGroup = groupedFrequencies.GetValueOrDefault(HaplotypeTypingCategory.SmallGGroup, []),
        };
    }

    // Same eventual-consistency reasoning as projectedPool above: built eagerly in production by
    // HaplotypeFrequencyCache.BuildEntryFromDatabase, so the volatile/??= pair only matters for a caller that
    // constructs an entry directly, such as a test.
    private volatile DataByResolution<LociInfo<int[][]>> alleleIndex;

    /// <summary>
    /// Per (category, locus, allele id): the ascending <see cref="ProjectedPool"/> positions of haplotypes carrying
    /// that id at that locus.
    ///
    /// <para>
    /// This lets <c>CompressedPhenotypeExpander.GetHaplotypesForAllowedLoci</c> visit only the haplotypes a subject's
    /// own typing can possibly explain, instead of testing every pooled haplotype. It is built once per set, as a
    /// pure function of <see cref="ProjectedPool"/> - so it inherits the same "first donor pays, every donor after is
    /// ~0" cost shape and, because it is derived from the pool rather than from <see cref="SetFrequencies"/> directly,
    /// the same pool order.
    /// </para>
    ///
    /// <para>
    /// <b>Ids stop here</b>, exactly as for <see cref="ProjectedPool"/>: they are meaningful only against
    /// <see cref="Interner"/>, and never leave this object.
    /// </para>
    /// </summary>
    internal DataByResolution<LociInfo<int[][]>> AlleleIndex => alleleIndex ??= ProjectedPool.Map(BuildIndexForCategory);

    private LociInfo<int[][]> BuildIndexForCategory(HaplotypeKey[] haplotypes)
    {
        // Named args, as HaplotypeInterner.ReverseLookup does: LociInfo's positional constructor is
        // (A, B, C, Dpb1, Dqb1, Drb1), and a haplotype carries no Dpb1 - the Dpb1 slot here is simply never read,
        // since callers only ever index by LocusSettings.MatchPredictionLoci.
        return new LociInfo<int[][]>(
            valueA: BuildIndexForLocus(haplotypes, Locus.A, Interner.A),
            valueB: BuildIndexForLocus(haplotypes, Locus.B, Interner.B),
            valueC: BuildIndexForLocus(haplotypes, Locus.C, Interner.C),
            valueDqb1: BuildIndexForLocus(haplotypes, Locus.Dqb1, Interner.Dqb1),
            valueDrb1: BuildIndexForLocus(haplotypes, Locus.Drb1, Interner.Drb1));
    }

    // Dense, exactly as BuildAllowedAlleleMasks's masks are: AlleleInterner mints ids from 0, so an array indexed by
    // id needs no hashing to build or to read. Each haplotypes[i] is visited once, in order, so every bucket comes
    // out ascending for free - which is what lets the caller merge a handful of buckets and stay in pool order.
    private static int[][] BuildIndexForLocus(HaplotypeKey[] haplotypes, Locus locus, AlleleInterner alleles)
    {
        var buckets = new List<int>[alleles.IdCount];

        for (var i = 0; i < haplotypes.Length; i++)
        {
            var id = haplotypes[i].GetLocus(locus);
            (buckets[id] ??= []).Add(i);
        }

        var index = new int[buckets.Length][];

        for (var id = 0; id < buckets.Length; id++)
        {
            index[id] = buckets[id]?.ToArray() ?? [];
        }

        return index;
    }
}
