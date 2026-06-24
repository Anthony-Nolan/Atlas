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
using HaplotypeHla = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.LociInfo<string>;

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
    /// Returns the cached entry for the set, loading it from the database on first access. Loading also kicks off
    /// the (slower) background pre-consolidation, which populates <see cref="FrequencySetCacheEntry.ConsolidatedFrequencies"/>
    /// on the same entry once complete.
    /// </summary>
    Task<FrequencySetCacheEntry> GetAllHaplotypeFrequencies(int setId);

    /// <summary>
    /// Returns the consolidated frequency for the given haplotype/excluded loci.
    /// If the full consolidated collection has finished warming, reads the value from it.
    /// Otherwise calculates this single value directly from the (already in-memory) frequency set,
    /// so the first caller is not blocked on the significantly slower full pre-consolidation.
    /// </summary>
    Task<decimal> GetConsolidatedFrequency(int setId, HaplotypeHla hla, ISet<Locus> excludedLoci);
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

                // The same entry instance is what gets cached, so the background task populates exactly the object
                // future callers will read. It is deliberately not awaited - the full pre-consolidation is slow and
                // must not delay the first lookup, which falls back to a direct calculation while ConsolidatedFrequencies is null.
                _ = Task.Run(() => WarmConsolidatedFrequencies(setId, entry));

                return entry;
            }
        );
    }

    /// <inheritdoc />
    public async Task<decimal> GetConsolidatedFrequency(int setId, HaplotypeHla hla, ISet<Locus> excludedLoci)
    {
        var entry = await GetAllHaplotypeFrequencies(setId);

        // The whole collection has finished warming: read the value straight from it.
        if (entry.ConsolidatedFrequencies != null)
        {
            return ReadConsolidatedFrequency(entry, hla, excludedLoci);
        }

        // Still warming (or warming failed): calculate this single value directly. This is pure in-memory work over
        // the already-cached set - no SQL connection - so it needs no concurrency throttling.
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

            return new FrequencySetCacheEntry
            {
                SetFrequencies = resultDictionary.ToFrozenDictionary(),
                Interner = haplotypeInterner
            };
        }
    }

    private decimal ReadConsolidatedFrequency(FrequencySetCacheEntry entry, HaplotypeHla hla, ISet<Locus> excludedLoci)
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
