using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
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

    // volatile, and assigned with ??= rather than under a lock: the read-test-write is not atomic, so two threads
    // racing the first access can both project - but a FrozenDictionary enumerates in a stable order, so the two
    // results are element-for-element identical and either may win. A reference write has release semantics, so the
    // collection is fully built before any thread can observe it.
    private volatile DataByResolution<HaplotypeKey[]> projectedPool;

    /// <summary>
    /// The set's haplotypes in the string form phenotype expansion filters against, grouped by typing category.
    ///
    /// <para>
    /// ATL-233 T1. This projection is a pure function of <see cref="SetFrequencies"/> and <see cref="Interner"/> -
    /// <c>CompressedPhenotypeExpander.FetchHaplotypesGroupedByTypingCategory</c> takes a set id and nothing else, not
    /// even the allowed loci - yet it ran once per DONOR, walking up to 274,606 frozen entries and allocating one
    /// <see cref="LociInfo{T}"/> (with its eagerly computed hash) per haplotype. Measured at 5.925 ms per donor and
    /// <b>invariant across all four AllowedLoci keys</b>, because it projects the whole set however small the
    /// question: 28.5% of blended Impute time, and 44.60% of the 3-locus key. Memoising it here is the largest single
    /// item in the ticket pack, and the only large one that cannot change a clinical result.
    /// </para>
    ///
    /// <para>
    /// It lives on the cache entry because the entry already owns both inputs and already has the per-set lifetime
    /// this needs: one <c>IAppCache</c> entry per set id, so the pool cannot outlive, or drift from, the frequencies
    /// it was derived from.
    /// </para>
    ///
    /// <para>
    /// <b>ATL-233 T1 follow-up: the pool is the set's own interned keys, not their name form.</b> T1 cached this as
    /// <c>LociInfo&lt;string&gt;</c> - ~80 bytes per haplotype against ~55 for a <see cref="SetFrequencies"/> entry, so
    /// about 2.5x the footprint of a cached set, ~22 MB for the largest and a real claim on a 4Gi replica. A
    /// <see cref="HaplotypeKey"/> is 20 bytes in a flat array, which hands most of that back (~5.5 MB), and building it
    /// no longer calls <c>ReverseLookup</c> per haplotype: the keys are already what the frozen dictionary holds. The
    /// filter then compares ids instead of hashing allele names, and only the survivors are resolved back to names.
    /// </para>
    ///
    /// <para>
    /// <b>The ids never leave this object.</b> They are meaningful only against <see cref="Interner"/>, and a second
    /// entry for the same set - built after eviction or expiry - has a different id space. That is safe here because
    /// the pool, the frequencies and the interner are fields of one immutable entry, exactly as the class remark above
    /// states; it is <i>not</i> safe to pass an id to anything that re-enters the cache, such as
    /// <c>HaplotypeFrequencyService.GetFrequencyForHla</c>, which fetches the entry again and may hold a different
    /// instance. Survivors are therefore carried onwards as names, as they always were.
    /// </para>
    /// </summary>
    internal DataByResolution<HaplotypeKey[]> ProjectedPool => projectedPool ??= ProjectPool();

    /// <summary>
    /// The shipped expression's <b>enumeration</b>, unchanged - same source, same grouping, same order. Only what it
    /// yields per haplotype changed, from the <c>ReverseLookup</c>'d name form to the key the dictionary already holds.
    ///
    /// <para>
    /// It is tempting to build this straight from the SQL rows in <c>BuildEntryFromDatabase</c>'s existing loop, which
    /// would skip this pass altogether. Do not. The pool's order sets the survivor order, which sets
    /// the pairing order, which sets the genotype set's insertion order - and
    /// <c>ExpandedGenotypeTruncater.TruncateGenotypes</c> keeps the top N with an <c>OrderByDescending</c> that has
    /// <b>no secondary key</b> (the only <c>OrderBy</c> in this project, with no genotype comparer anywhere). So a
    /// different enumeration order can silently change which genotypes a capped donor keeps when likelihoods tie:
    /// a clinical change needing HLA-expert sign-off, not a performance one. Projecting from the frozen dictionary,
    /// exactly as before, keeps the order identical.
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
}
