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
    private volatile DataByResolution<IReadOnlyCollection<LociInfo<string>>> projectedPool;

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
    /// <b>Cost.</b> This trades per-donor allocation churn for resident memory - roughly 80 bytes per haplotype
    /// against ~55 for a <see cref="SetFrequencies"/> entry, so about 2.5x the footprint of a cached set. Deliberate:
    /// GC Wait was measured at 44.3% of CPU-active time against 15.6% for user code, so removing the churn is
    /// expected to pay for the residency. It is nevertheless a real claim on a 4Gi replica's memory budget, and one
    /// that grows with the number of distinct sets a replica touches.
    /// </para>
    /// </summary>
    internal DataByResolution<IReadOnlyCollection<LociInfo<string>>> ProjectedPool => projectedPool ??= ProjectPool();

    /// <summary>
    /// Deliberately the shipped expression, moved rather than rewritten.
    ///
    /// <para>
    /// It is tempting to build this straight from the SQL rows in <c>BuildEntryFromDatabase</c>'s existing loop, which
    /// would delete the <c>ReverseLookup</c> entirely. Do not. The pool's order sets the survivor order, which sets
    /// the pairing order, which sets the genotype set's insertion order - and
    /// <c>ExpandedGenotypeTruncater.TruncateGenotypes</c> keeps the top N with an <c>OrderByDescending</c> that has
    /// <b>no secondary key</b> (the only <c>OrderBy</c> in this project, with no genotype comparer anywhere). So a
    /// different enumeration order can silently change which genotypes a capped donor keeps when likelihoods tie:
    /// a clinical change needing HLA-expert sign-off, not a performance one. Projecting from the frozen dictionary,
    /// exactly as before, keeps the order identical.
    /// </para>
    /// </summary>
    private DataByResolution<IReadOnlyCollection<LociInfo<string>>> ProjectPool()
    {
        var groupedFrequencies = SetFrequencies
            .GroupBy(f => f.Value.TypingCategory)
            .ToDictionary(
                key => key.Key,
                value => value.Select(f => Interner.ReverseLookup(f.Key)).ToList()
            );

        return new DataByResolution<IReadOnlyCollection<LociInfo<string>>>
        {
            GGroup = groupedFrequencies.GetValueOrDefault(HaplotypeTypingCategory.GGroup, []),
            PGroup = groupedFrequencies.GetValueOrDefault(HaplotypeTypingCategory.PGroup, []),
            SmallGGroup = groupedFrequencies.GetValueOrDefault(HaplotypeTypingCategory.SmallGGroup, []),
        };
    }
}
