using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Caching;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;
// ONE haplotype's names - one per locus, at the resolution the frequency set stored them. A LociInfo, not a
// PhenotypeInfo: the genotype form (HfSetGenotypeNames) holds two names per locus, being a pair of these.
using HfSetHaplotypeNames = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.LociInfo<string>;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies;

/// <summary>
/// Owns all caching of haplotype frequency data: the active set lookup and the per-set <see cref="FrequencySetCacheEntry"/>
/// (the full frequency collection, its interner, and the pre-consolidated missing-loci frequencies).
/// Encapsulating the cache keys and the "serve a single value while the whole collection warms in the background"
/// workflow here keeps <see cref="HaplotypeFrequencyService"/> free of cache plumbing and lambda passing.
/// </summary>
public interface IHaplotypeFrequencyCache
{
    Task<IReadOnlyDictionary<(string RegistryCode, string EthnicityCode), HaplotypeFrequencySet>> GetActiveHaplotypeFrequencySets();

    /// <summary>Invalidates the active set cache, e.g. after a new set has been imported.</summary>
    void RemoveActiveHaplotypeFrequencySets();

    /// <summary>
    /// Returns the cached entry for the set, loading it from the database on first access. Loading also runs the
    /// (slower) missing-loci pre-consolidation, which populates <see cref="FrequencySetCacheEntry.ConsolidatedFrequencies"/>
    /// on the same entry.
    ///
    /// <para>
    /// Whether this method <b>waits</b> for that is
    /// <c>HaplotypeFrequencySetCacheSettings.AwaitConsolidatedFrequencyWarm</c>. False - the default - returns as soon
    /// as the set is loaded and lets the pre-consolidation finish in the background, so a caller arriving during the
    /// warm falls back to a direct per-haplotype scan. True returns only once the collection is ready, which is what a
    /// precompute wants and a search does not.
    /// </para>
    /// </summary>
    Task<FrequencySetCacheEntry> GetAllHaplotypeFrequencies(int setId);

    /// <summary>
    /// Returns the consolidated frequency for the given haplotype/excluded loci.
    /// If the full consolidated collection has finished warming, reads the value from it.
    /// Otherwise calculates this single value directly from the (already in-memory) frequency set,
    /// so the first caller is not blocked on the significantly slower full pre-consolidation.
    /// </summary>
    Task<decimal> GetConsolidatedFrequency(int setId, HfSetHaplotypeNames hla, ISet<Locus> excludedLoci);
}

internal class HaplotypeFrequencyCache : IHaplotypeFrequencyCache
{
    private const string ActiveHaplotypeFrequencySetsCacheKey = "hf-active-sets";

    private static string AllFrequenciesCacheKey(int setId) => $"hf-set-{setId}";

    private readonly IAppCache cache;
    private readonly IHaplotypeFrequenciesRepository frequencyRepository;
    private readonly IHaplotypeFrequencySetRepository frequencySetRepository;
    private readonly IFrequencyConsolidator frequencyConsolidator;
    private readonly IAtlasLogger logger;
    private readonly HaplotypeFrequencySetCacheSettings cacheSettings;

    public HaplotypeFrequencyCache(
        IPersistentCacheProvider persistentCacheProvider,
        IHaplotypeFrequenciesRepository frequencyRepository,
        IHaplotypeFrequencySetRepository frequencySetRepository,
        IFrequencyConsolidator frequencyConsolidator,
        IMatchPredictionLogger<MatchProbabilityLoggingContext> logger,
        IOptions<HaplotypeFrequencySetCacheSettings> cacheSettings)
    {
        cache = persistentCacheProvider.Cache;
        this.frequencyRepository = frequencyRepository;
        this.frequencySetRepository = frequencySetRepository;
        this.frequencyConsolidator = frequencyConsolidator;
        this.logger = logger;
        this.cacheSettings = cacheSettings.Value;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<(string RegistryCode, string EthnicityCode), HaplotypeFrequencySet>> GetActiveHaplotypeFrequencySets()
    {
        return await cache.GetOrAddAsync(
            ActiveHaplotypeFrequencySetsCacheKey,
            async () =>
            {
                using (logger.RunTimed("Get active HF sets - from SQL database", LogLevel.Verbose))
                {
                    var activeSets = await frequencySetRepository.GetAllActiveSets();
                    return activeSets.ToDictionary(
                        set => (set.RegistryCode, set.EthnicityCode),
                        MapDataModelToClientModel
                    );
                }
            },
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheSettings.ActiveSetCacheExpiryMinutes)
            }
        );
    }

    /// <inheritdoc />
    public void RemoveActiveHaplotypeFrequencySets() => cache.Remove(ActiveHaplotypeFrequencySetsCacheKey);

    /// <inheritdoc />
    public async Task<FrequencySetCacheEntry> GetAllHaplotypeFrequencies(int setId)
    {
        return await cache.GetOrAddAsync(AllFrequenciesCacheKey(setId), async () =>
            {
                var entry = await BuildEntryFromDatabase(setId);

                // The same entry instance is what gets warmed either way, so the pre-consolidation populates exactly
                // the object future callers will read. Exactly one writer in both modes - the branch decides only
                // whether that writer is on the critical path.
                if (cacheSettings.AwaitConsolidatedFrequencyWarm)
                {
                    // Inside the GetOrAddAsync factory, so the wait happens once per set and every concurrent caller
                    // for the same set awaits the same lazy task rather than racing it. When this returns,
                    // ConsolidatedFrequencies is populated and no caller can reach the direct-scan fallback, which is
                    // the whole point - a scan costs roughly a thousand times a warm read.
                    WarmConsolidatedFrequencies(setId, entry);
                }
                else
                {
                    // The right behaviour where a request is waiting: the full pre-consolidation is slow and must not
                    // delay the first lookup, which falls back to a direct calculation while ConsolidatedFrequencies
                    // is null.
                    _ = Task.Run(() => WarmConsolidatedFrequencies(setId, entry));
                }

                return entry;
            }
        );
    }

    /// <inheritdoc />
    public async Task<decimal> GetConsolidatedFrequency(int setId, HfSetHaplotypeNames hla, ISet<Locus> excludedLoci)
    {
        var entry = await GetAllHaplotypeFrequencies(setId);

        // The whole collection has finished warming: read the value straight from it.
        if (entry.ConsolidatedFrequencies != null)
        {
            return ReadConsolidatedFrequency(entry, hla, excludedLoci);
        }

        // Still warming (or warming failed): calculate this single value directly. This is pure in-memory work over
        // the already-cached set - no SQL connection - so it needs no concurrency throttling.
        //
        // "Pure in-memory" is not the same as cheap: this is a full linear scan of SetFrequencies with a RemoveLoci
        // allocation per entry, for every haplotype that lands here. Callers that resolve many frequencies for one
        // subject can therefore pay it many times over while the warm is still running.
        return frequencyConsolidator.ConsolidateFrequenciesForHaplotype(entry, hla, excludedLoci);
    }

    private async Task<FrequencySetCacheEntry> BuildEntryFromDatabase(int setId)
    {
        using (logger.RunTimed("Get All Frequencies from HF set - from SQL database", LogLevel.Verbose))
        {
            var allFrequencies = await frequencyRepository.GetAllHaplotypeFrequencies(setId);
            var haplotypeInterner = new HaplotypeInterner();
            var resultDictionary = new Dictionary<HaplotypeKey, HaplotypeFrequencyValue>();
            foreach (var frequency in allFrequencies)
            {
                var haplotypeKey = haplotypeInterner.Intern(a: frequency.A, b: frequency.B, c: frequency.C, dqb1: frequency.DQB1, drb1: frequency.DRB1);
                var haplotypeFrequencyValue = new HaplotypeFrequencyValue(frequency.Frequency, frequency.TypingCategory);
                resultDictionary.Add(haplotypeKey, haplotypeFrequencyValue);
            }

            var entry = new FrequencySetCacheEntry
            {
                SetFrequencies = resultDictionary.ToFrozenDictionary(),
                Interner = haplotypeInterner
            };

            // Project the pool here, on the one thread that builds the set, rather than leaving the first donor to
            // touch this set to pay for it. Inside the timed region because it is part of the cost of making a set
            // usable, and before the entry escapes, so no reader can race the first access.
            _ = entry.ProjectedPool;

            return entry;
        }
    }

    private decimal ReadConsolidatedFrequency(FrequencySetCacheEntry entry, HfSetHaplotypeNames hla, ISet<Locus> excludedLoci)
    {
        var keyToSeek = entry.Interner.ConvertWherePossible(hla.A, hla.B, hla.C, hla.Dqb1, hla.Drb1);
        keyToSeek = keyToSeek.RemoveLoci(excludedLoci.ToArray());
        entry.ConsolidatedFrequencies.TryGetValue(keyToSeek, out var result);
        return result;
    }

    private void WarmConsolidatedFrequencies(int setId, FrequencySetCacheEntry entry)
    {
        try
        {
            // It is significantly faster to calculate all consolidated values up front than to calculate on the fly, even when caching individual values.
            // Many consolidated haplotypes may be inferable from the input data, but not actually represented in the haplotype frequency dataset.
            using (logger.RunTimed($"Calculating consolidated frequencies with missing loci for set: {setId}"))
            {
                entry.ConsolidatedFrequencies = frequencyConsolidator.PreConsolidateFrequenciesForCommonMissingLoci(entry);
            }
        }
        catch (Exception e)
        {
            logger.SendTrace($"Failed to warm consolidated frequency cache for set {setId}: {e.Message}", LogLevel.Error);
        }
    }

    private static HaplotypeFrequencySet MapDataModelToClientModel(Data.Models.HaplotypeFrequencySet set)
    {
        return new HaplotypeFrequencySet
        {
            HlaNomenclatureVersion = set.HlaNomenclatureVersion,
            EthnicityCode = set.EthnicityCode,
            Id = set.Id,
            Name = set.Name,
            RegistryCode = set.RegistryCode,
            PopulationId = set.PopulationId
        };
    }
}
