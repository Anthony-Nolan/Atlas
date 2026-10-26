using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.Caching;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Data.Models;
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
    /// <summary>
    /// The active sets, by registry and ethnicity code. Each process keeps them for
    /// <c>HaplotypeFrequencySetCacheSettings.ActiveSetCacheExpiryMinutes</c>, and then reads them from the distributed
    /// cache, which all processes share. So a change that <see cref="RefreshActiveHaplotypeFrequencySets"/> stores reaches
    /// every process within that time.
    /// </summary>
    /// <remarks>
    /// The result is a snapshot: an operation that must use one set of active sets from start to end can keep it.
    /// </remarks>
    Task<IReadOnlyDictionary<(string RegistryCode, string EthnicityCode), HaplotypeFrequencySet>> GetActiveHaplotypeFrequencySets();

    /// <summary>
    /// Reads the active sets from the database, and stores them in this process and in the distributed cache, e.g. after
    /// a new set has been imported. This process uses them at once.
    /// </summary>
    /// <returns>
    /// <c>false</c> if the distributed cache did not store them. Other processes can then use the replaced sets until the
    /// sets in the distributed cache expire.
    /// </returns>
    Task<bool> RefreshActiveHaplotypeFrequencySets();

    /// <summary>
    /// Returns the cached entry for the set, loading it on first access: from the distributed cache, which all processes
    /// share, or else from the database. Loading also runs the (slower) missing-loci pre-consolidation, which populates
    /// <see cref="FrequencySetCacheEntry.ConsolidatedFrequencies"/> on the same entry.
    ///
    /// <para>
    /// While the set is in use, this process checks its row in the database each
    /// <c>HaplotypeFrequencySetCacheSettings.ActiveSetCacheExpiryMinutes</c>, and loads the set again when the row has
    /// another import time, or no longer exists: after a restore or a copy of the database, a set id can name another set.
    /// </para>
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

    /// <summary>
    /// <see cref="GetConsolidatedFrequency(int, HfSetHaplotypeNames, ISet{Locus})"/> for an entry the caller already
    /// holds, so a caller resolving many frequencies fetches the set once.
    /// </summary>
    decimal GetConsolidatedFrequency(FrequencySetCacheEntry entry, HfSetHaplotypeNames hla, ISet<Locus> excludedLoci);
}

internal class HaplotypeFrequencyCache : IHaplotypeFrequencyCache
{
    private const string ActiveHaplotypeFrequencySetsCacheKey = "hf-active-sets";

    /// <summary>
    /// The key of the active sets in the distributed cache. Change the version in the cache name when the stored format
    /// changes: during a deployment, the old and the new code read the same cache.
    /// </summary>
    internal static readonly DistributedCacheKey ActiveHaplotypeFrequencySetsDistributedCacheKey = new("hf-active-sets-v1", "all");

    /// <summary>
    /// How long the distributed cache keeps the active sets. Each change made through this class is written to it, so this
    /// is only how long a change that was not written can stay unseen - e.g. a set activated by hand, with SQL.
    /// </summary>
    internal static readonly TimeSpan ActiveHaplotypeFrequencySetsDistributedCacheTimeToLive = TimeSpan.FromHours(24);

    internal static string AllFrequenciesCacheKey(int setId) => $"hf-set-{setId}";

    /// <summary>The local key of the last check of a set against its row in the database: see <see cref="IsReplaced"/>.</summary>
    internal static string SetCheckCacheKey(int setId) => $"hf-set-checked-{setId}";

    /// <summary>
    /// How long the check of a set waits for the row of the set. All callers of the set wait for the check, but a loaded set
    /// needs no database: a check that takes longer stops, and the set stays in use until the next check. Without this
    /// limit, the retries of the database context and its connection timeout could hold the callers for minutes when SQL
    /// is slow or cannot be reached.
    /// </summary>
    internal static readonly TimeSpan SetCheckTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The cache name of the frequency sets in the distributed cache. Change the version when the format of
    /// <see cref="FrequencySetPayload"/> changes: during a deployment, the old and the new code read the same cache.
    /// </summary>
    internal const string FrequencySetDistributedCacheName = "hf-set-v1";

    /// <summary>
    /// How long the distributed cache keeps a frequency set. The value under a key never changes, so this only frees the
    /// memory of the sets that are no longer used.
    /// </summary>
    internal static readonly TimeSpan FrequencySetDistributedCacheTimeToLive = TimeSpan.FromDays(30);

    /// <summary>
    /// The key of a set in the distributed cache. It has the import time of the set as well as its id: set ids are SQL
    /// identity values, so after a restore, a copy or a rebuild of the database, one id can name a different set.
    /// </summary>
    internal static DistributedCacheKey FrequencySetDistributedCacheKey(Data.Models.HaplotypeFrequencySet set) =>
        new(FrequencySetDistributedCacheName, $"{set.Id}:{set.DateTimeAdded.UtcTicks}");

    private readonly IAppCache cache;
    private readonly IDistributedCacheProvider distributedCache;
    private readonly IFrequencySetResidencyTracker residencyTracker;
    private readonly IHaplotypeFrequenciesRepository frequencyRepository;
    private readonly IHaplotypeFrequencySetRepository frequencySetRepository;
    private readonly IFrequencyConsolidator frequencyConsolidator;
    private readonly IAtlasLogger logger;
    private readonly HaplotypeFrequencySetCacheSettings cacheSettings;
    private readonly TimeProvider timeProvider;

    public HaplotypeFrequencyCache(
        IHaplotypeFrequencySetCacheProvider cacheProvider,
        IDistributedCacheProvider distributedCache,
        IFrequencySetResidencyTracker residencyTracker,
        IHaplotypeFrequenciesRepository frequencyRepository,
        IHaplotypeFrequencySetRepository frequencySetRepository,
        IFrequencyConsolidator frequencyConsolidator,
        IMatchPredictionLogger<MatchProbabilityLoggingContext> logger,
        IOptions<HaplotypeFrequencySetCacheSettings> cacheSettings,
        TimeProvider timeProvider)
    {
        cache = cacheProvider.Cache;
        this.distributedCache = distributedCache;
        this.residencyTracker = residencyTracker;
        this.frequencyRepository = frequencyRepository;
        this.frequencySetRepository = frequencySetRepository;
        this.frequencyConsolidator = frequencyConsolidator;
        this.logger = logger;
        this.cacheSettings = cacheSettings.Value;
        this.timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<(string RegistryCode, string EthnicityCode), HaplotypeFrequencySet>> GetActiveHaplotypeFrequencySets()
    {
        // GetOrAddAsync runs one factory at a time for the key, and concurrent callers wait for it. So when the distributed
        // cache misses or cannot be reached, this process reads the database once for each local entry, not once for each
        // caller.
        return await cache.GetOrAddAsync(
            ActiveHaplotypeFrequencySetsCacheKey,
            async () =>
            {
                var storedSets = await distributedCache.GetAsync(ActiveHaplotypeFrequencySetsDistributedCacheKey);
                if (storedSets != null)
                {
                    return ByRegistryAndEthnicity(JsonSerializer.Deserialize<List<HaplotypeFrequencySet>>(storedSets));
                }

                var activeSets = await GetActiveSetsFromDatabase();

                // Sets that are not stored are no fault here: the next local entry reads the database again.
                await distributedCache.SetAsync(
                    ActiveHaplotypeFrequencySetsDistributedCacheKey,
                    JsonSerializer.SerializeToUtf8Bytes(activeSets),
                    ActiveHaplotypeFrequencySetsDistributedCacheTimeToLive);

                return ByRegistryAndEthnicity(activeSets);
            },
            ActiveSetsLocalEntryOptions()
        );
    }

    /// <inheritdoc />
    public async Task<bool> RefreshActiveHaplotypeFrequencySets()
    {
        var activeSets = await GetActiveSetsFromDatabase();

        // Straight into this process's cache, not through the distributed cache: this process uses the new sets at once,
        // also when the distributed cache does not store them.
        cache.Add(ActiveHaplotypeFrequencySetsCacheKey, ByRegistryAndEthnicity(activeSets), ActiveSetsLocalEntryOptions());

        return await distributedCache.SetAsync(
            ActiveHaplotypeFrequencySetsDistributedCacheKey,
            JsonSerializer.SerializeToUtf8Bytes(activeSets),
            ActiveHaplotypeFrequencySetsDistributedCacheTimeToLive);
    }

    /// <inheritdoc />
    public async Task<FrequencySetCacheEntry> GetAllHaplotypeFrequencies(int setId)
    {
        // Marks setId most-recently-used. A load evicts the least-recently-used sets when it records the size of the set.
        // See FrequencySetResidencyTracker's doc comment for why eviction has to be driven explicitly like this.
        residencyTracker.RecordAccess(setId);

        var entry = await GetOrLoadEntry(setId);

        // The check evicted the entry: its set id names another set in the database now, or no set. Load that.
        if (await IsReplaced(setId))
        {
            entry = await GetOrLoadEntry(setId);
        }

        return entry;
    }

    private async Task<FrequencySetCacheEntry> GetOrLoadEntry(int setId)
    {
        // GetOrAddAsync runs one factory at a time for the key, and concurrent callers wait for it. So when the distributed
        // cache misses or cannot be reached, this process reads each set from the database once at a time, not once for
        // each caller.
        return await cache.GetOrAddAsync(AllFrequenciesCacheKey(setId), async cacheEntry =>
            {
                // An earlier entry of the set has left the cache, but its post-eviction callback can still wait on the
                // thread pool. Until RecordSize, the set holds no bytes, so the tracker cannot evict this entry while it
                // loads. It must not: no callback is registered on this entry yet, so its bytes would stay counted.
                residencyTracker.RecordLoadStart(setId);

                var (entry, importTime) = await LoadEntry(setId);

                var admission = residencyTracker.RecordSize(setId, entry.EstimatedSizeInBytes, importTime);

                // Fires for every removal reason - expiry, or an explicit Remove, including the tracker's own onEvict →
                // cache.Remove call. In that last case this is a harmless no-op, since the tracker already removed setId
                // from its own bookkeeping before calling onEvict. Without this, an expiry would leave the tracker still
                // counting setId as resident - see FrequencySetResidencyTracker.Forget's doc comment. Registered here,
                // not in the options, so that it carries the admission of this entry.
                cacheEntry.RegisterPostEvictionCallback((_, _, _, _) => residencyTracker.Forget(setId, admission));

                // The load has just read the row of the set: the next check of the set is due an interval from now.
                cache.Add(SetCheckCacheKey(setId), false, SetCheckLocalEntryOptions());

                // The same entry instance is what gets warmed either way, so the pre-consolidation populates exactly
                // the object future callers will read. Exactly one writer in both modes - the branch decides only
                // whether that writer is on the critical path.
                if (cacheSettings.AwaitConsolidatedFrequencyWarm)
                {
                    // Inside the GetOrAddAsync factory, so the wait happens once per set and every concurrent caller
                    // for the same set awaits the same lazy task rather than racing it. When this returns,
                    // ConsolidatedFrequencies is populated and no caller can reach the direct-scan fallback, which is
                    // the whole point - a scan costs roughly a thousand times a warm read. A failure here propagates
                    // out of this factory instead of being swallowed, so GetOrAddAsync does not cache a half-warmed
                    // entry and the next caller for this set retries from scratch.
                    WarmConsolidatedFrequencies(setId, entry, rethrowOnFailure: true);
                }
                else
                {
                    // The right behaviour where a request is waiting: the full pre-consolidation is slow and must not
                    // delay the first lookup, which falls back to a direct calculation while ConsolidatedFrequencies
                    // is null.
                    _ = Task.Run(() => WarmConsolidatedFrequencies(setId, entry, rethrowOnFailure: false));
                }

                return entry;
            },
            SetLocalEntryOptions()
        );
    }

    /// <summary>
    /// Checks the loaded set against its row in the database, once each <c>ActiveSetCacheExpiryMinutes</c> while the set is
    /// in use, and evicts it when the row has another import time, or no longer exists. Set ids are SQL identity values:
    /// after a restore or a copy of the database, the id of a loaded set can name another set. A set in use does not
    /// expire, so without the check, this process would use the old frequencies until it restarts.
    /// </summary>
    /// <returns>Whether the check evicted the set, so that the caller loads it again.</returns>
    private async Task<bool> IsReplaced(int setId)
    {
        // One factory at a time for the key, as for the entry: so one caller evicts the set, not each caller, and no later
        // caller can evict the entry that replaces it.
        return await cache.GetOrAddAsync(SetCheckCacheKey(setId), async () =>
            {
                DateTimeOffset? importTime;
                try
                {
                    using var timeout = new CancellationTokenSource(SetCheckTimeout, timeProvider);
                    importTime = (await frequencySetRepository.GetSet(setId, timeout.Token))?.DateTimeAdded;
                }
                catch (Exception exception)
                {
                    // A loaded set needs no database, so a fault or a timeout here does not fail the caller: the set stays
                    // in use, and the next check is due an interval from now.
                    var reason = exception is OperationCanceledException ? $"no answer in {SetCheckTimeout.TotalSeconds} s" : exception.Message;
                    logger.SendTrace(
                        $"HF set {setId} could not be checked against its row in the database, so this process keeps it until the next check: {reason}",
                        LogLevel.Warn);
                    return false;
                }

                // The tracker compares it with the import time of the loaded entry. It does not evict a set that is loading:
                // that load reads the current row.
                if (!residencyTracker.EvictIfReplaced(setId, importTime))
                {
                    return false;
                }

                logger.SendTrace($"HF set {setId} has another import time in the database, or no row, so this process loads it again.");
                return true;
            },
            SetCheckLocalEntryOptions()
        );
    }

    /// <inheritdoc />
    public async Task<decimal> GetConsolidatedFrequency(int setId, HfSetHaplotypeNames hla, ISet<Locus> excludedLoci)
    {
        var entry = await GetAllHaplotypeFrequencies(setId);
        return GetConsolidatedFrequency(entry, hla, excludedLoci);
    }

    /// <inheritdoc />
    public decimal GetConsolidatedFrequency(FrequencySetCacheEntry entry, HfSetHaplotypeNames hla, ISet<Locus> excludedLoci)
    {
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

    private async Task<List<HaplotypeFrequencySet>> GetActiveSetsFromDatabase()
    {
        using (logger.RunTimed("Get active HF sets - from SQL database", LogLevel.Verbose))
        {
            var activeSets = await frequencySetRepository.GetAllActiveSets();
            return activeSets.Select(MapDataModelToClientModel).ToList();
        }
    }

    private static IReadOnlyDictionary<(string RegistryCode, string EthnicityCode), HaplotypeFrequencySet> ByRegistryAndEthnicity(
        IEnumerable<HaplotypeFrequencySet> activeSets) =>
        activeSets.ToDictionary(set => (set.RegistryCode, set.EthnicityCode));

    /// <summary>An absolute expiry, not a sliding one: a process that reads the sets all the time still reads them again.</summary>
    private MemoryCacheEntryOptions ActiveSetsLocalEntryOptions() => new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheSettings.ActiveSetCacheExpiryMinutes)
    };

    /// <summary>
    /// A sliding expiry, not an absolute one: a set that is in use stays loaded, and a set with no use for the expiry
    /// goes. The residency tracker evicts the least-recently-used sets before that, when the memory limit needs it.
    /// </summary>
    private MemoryCacheEntryOptions SetLocalEntryOptions() => new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(cacheSettings.SetCacheExpiryMinutes)
    };

    /// <summary>
    /// An absolute expiry of <c>ActiveSetCacheExpiryMinutes</c>, as for the active sets: so a set that a restore or a copy
    /// of the database replaced reaches this process in the same time as the list of active sets that names it.
    /// </summary>
    private MemoryCacheEntryOptions SetCheckLocalEntryOptions() => new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheSettings.ActiveSetCacheExpiryMinutes)
    };

    /// <summary>
    /// Loads a set from the distributed cache, or else from the database. A set from the database is then stored in the
    /// distributed cache if it is active.
    /// </summary>
    /// <returns>The entry, and the import time of its set: null when the set has no row.</returns>
    private async Task<(FrequencySetCacheEntry Entry, DateTimeOffset? ImportTime)> LoadEntry(int setId)
    {
        // One primary-key read: the import time of the set is part of its key in the distributed cache, and the check of
        // the set compares it later.
        var set = await frequencySetRepository.GetSet(setId);
        if (set == null)
        {
            // There are no rows to read either: the database read gives the empty entry, as it did before the
            // distributed cache.
            return ((await LoadEntryFromDatabase(setId)).Entry, null);
        }

        if (!distributedCache.IsEnabled)
        {
            // No Redis is configured: there is nothing to read from it, and nothing to encode for it.
            return ((await LoadEntryFromDatabase(setId)).Entry, set.DateTimeAdded);
        }

        var distributedCacheKey = FrequencySetDistributedCacheKey(set);
        var storedEntry = await LoadEntryFromDistributedCache(setId, distributedCacheKey);
        if (storedEntry != null)
        {
            return (storedEntry, set.DateTimeAdded);
        }

        var (rows, entry) = await LoadEntryFromDatabase(setId);

        // Only an active set. An inactive set can be the set of an import that still runs, with only part of its rows:
        // stored, that part would reach every process that uses the set after its activation.
        if (set.Active)
        {
            await StoreInDistributedCache(setId, distributedCacheKey, rows);
        }

        return (entry, set.DateTimeAdded);
    }

    /// <returns>The entry of the set in the distributed cache, or null on a miss.</returns>
    private async Task<FrequencySetCacheEntry> LoadEntryFromDistributedCache(int setId, DistributedCacheKey key)
    {
        var payload = await distributedCache.GetChunkedAsync(key);
        if (payload == null)
        {
            return null;
        }

        // The time of the reads from Redis is in the metrics of the distributed cache.
        using (logger.RunTimed("Get All Frequencies from HF set - from the distributed cache", LogLevel.Verbose))
        {
            List<LightweightHaplotypeFrequencyRecord> rows;
            try
            {
                rows = FrequencySetPayload.Decode(setId, payload);
            }
            catch (InvalidDataException exception)
            {
                // A miss: the set is read from the database, and stored again.
                logger.SendTrace($"HF set {setId} in the distributed cache cannot be decoded, so it is read from the database: {exception.Message}", LogLevel.Warn);
                return null;
            }

            return FrequencySetCacheEntry.Build(rows);
        }
    }

    private async Task<(List<LightweightHaplotypeFrequencyRecord> Rows, FrequencySetCacheEntry Entry)> LoadEntryFromDatabase(int setId)
    {
        using (logger.RunTimed("Get All Frequencies from HF set - from SQL database", LogLevel.Verbose))
        {
            var rows = await frequencyRepository.GetAllHaplotypeFrequencies(setId);
            return (rows, FrequencySetCacheEntry.Build(rows));
        }
    }

    private async Task StoreInDistributedCache(int setId, DistributedCacheKey key, IReadOnlyList<LightweightHaplotypeFrequencyRecord> rows)
    {
        var payload = FrequencySetPayload.Encode(setId, rows);

        // A set that is not stored is no fault here: the provider logged why, and the next process that misses the set
        // reads the database again.
        var stored = await distributedCache.SetChunkedAsync(key, payload, FrequencySetDistributedCacheTimeToLive);

        logger.SendTrace(
            $"HF set {setId} for the distributed cache: {rows.Count} rows, {payload.Length} bytes in {ChunkedDistributedCacheExtensions.ChunkCount(payload.Length)} chunks."
          + (stored ? "" : " Not stored."),
            LogLevel.Info);
    }

    private decimal ReadConsolidatedFrequency(FrequencySetCacheEntry entry, HfSetHaplotypeNames hla, ISet<Locus> excludedLoci)
    {
        var keyToSeek = entry.Interner.ConvertWherePossible(hla.A, hla.B, hla.C, hla.Dqb1, hla.Drb1);
        keyToSeek = keyToSeek.RemoveLoci(excludedLoci.ToArray());
        entry.ConsolidatedFrequencies.TryGetValue(keyToSeek, out var result);
        return result;
    }

    /// <param name="rethrowOnFailure">
    /// True when called on the awaited path: a caller of <see cref="GetAllHaplotypeFrequencies"/> asked to wait for
    /// the warm precisely so it wouldn't get back an entry whose <see cref="FrequencySetCacheEntry.ConsolidatedFrequencies"/>
    /// stays null forever. Swallowing here would leave every subsequent lookup silently falling back to the slow
    /// per-haplotype scan with no retry. False on the background (non-awaited) path, where there is no awaiter to
    /// propagate to and an unobserved task exception would be worse than a logged one.
    /// </param>
    private void WarmConsolidatedFrequencies(int setId, FrequencySetCacheEntry entry, bool rethrowOnFailure)
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

            if (rethrowOnFailure)
            {
                throw;
            }
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
