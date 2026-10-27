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
    /// Sets with the same content hash (<c>HaplotypeFrequencySets.ContentHash</c>) have the same rows, so they share one
    /// entry: this process loads and holds each content once. A set with no content hash has an entry of its own.
    /// </para>
    ///
    /// <para>
    /// While the set is in use, this process checks its row in the database each
    /// <c>HaplotypeFrequencySetCacheSettings.ActiveSetCacheExpiryMinutes</c>, and loads the set again when the row has
    /// another content hash or import time, or no longer exists: after a restore or a copy of the database, a set id can
    /// name another set.
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

    /// <summary>
    /// The local key of the entry of a content: the frequencies that all the sets with <paramref name="contentHash"/>
    /// share.
    /// </summary>
    internal static string ContentCacheKey(byte[] contentHash) => $"hf-set-content-{FrequencySetContentHash.ToHex(contentHash)}";

    /// <summary>
    /// The local key of the entry of a set that has no content hash, or whose rows do not have it: the frequencies of this
    /// set only. It has the import time of the set as well as its id: set ids are SQL identity values, so after a restore,
    /// a copy or a rebuild of the database, one id can name another set, which then gets another key.
    /// </summary>
    internal static string SetCacheKey(Data.Models.HaplotypeFrequencySet set) => $"hf-set-{set.Id}:{set.DateTimeAdded.UtcTicks}";

    /// <summary>The local key of the entry of a set id that has no row: the database gives no frequencies for it.</summary>
    internal static string MissingSetCacheKey(int setId) => $"hf-set-{setId}:none";

    /// <summary>The local key of the source of a set, which the last check of its row gave: see <see cref="GetSource"/>.</summary>
    internal static string SetCheckCacheKey(int setId) => $"hf-set-checked-{setId}";

    /// <summary>
    /// The local key of the last source of a set. It stays when the check entry expires, so a check that cannot read the
    /// row keeps it.
    /// </summary>
    private static string LastSourceCacheKey(int setId) => $"hf-set-source-{setId}";

    /// <summary>
    /// The local key of the quarantine of a set: the content hash in its row, which its rows do not have. While its row has
    /// this hash, the set uses its own rows: see <see cref="EnsureTheRowsHaveTheContentHash"/>.
    /// </summary>
    private static string QuarantineCacheKey(int setId) => $"hf-set-mismatch-{setId}";

    /// <summary>
    /// How long the check of a set waits for the row of the set. All callers of the set wait for the check, but a loaded set
    /// needs no database: a check that takes longer stops, and the set stays in use until the next check. Without this
    /// limit, the retries of the database context and its connection timeout could hold the callers for minutes when SQL
    /// is slow or cannot be reached.
    /// </summary>
    internal static readonly TimeSpan SetCheckTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The cache name of the sets with no content hash in the distributed cache: values in
    /// <see cref="FrequencySetPayload.FormatVersion"/>, each under the key of one set. Change the version when that format
    /// changes: during a deployment, the old and the new code read the same cache.
    /// </summary>
    internal const string FrequencySetDistributedCacheName = "hf-set-v1";

    /// <summary>
    /// The cache name of the contents in the distributed cache: values in
    /// <see cref="FrequencySetPayload.ContentFormatVersion"/>, each under a content hash, which all the sets with that hash
    /// share. Change the version when that format changes.
    /// </summary>
    internal const string ContentDistributedCacheName = "hf-set-v2";

    /// <summary>
    /// How long the distributed cache keeps a frequency set or a content. The value under a key never changes, so this only
    /// frees the memory of the values that are no longer used.
    /// </summary>
    internal static readonly TimeSpan FrequencySetDistributedCacheTimeToLive = TimeSpan.FromDays(30);

    /// <summary>
    /// The key of a set with no content hash in the distributed cache. It has the import time of the set as well as its id:
    /// set ids are SQL identity values, so after a restore, a copy or a rebuild of the database, one id can name a
    /// different set.
    /// </summary>
    internal static DistributedCacheKey FrequencySetDistributedCacheKey(Data.Models.HaplotypeFrequencySet set) =>
        new(FrequencySetDistributedCacheName, $"{set.Id}:{set.DateTimeAdded.UtcTicks}");

    /// <summary>The key of a content in the distributed cache: its content hash.</summary>
    internal static DistributedCacheKey ContentDistributedCacheKey(byte[] contentHash) =>
        new(ContentDistributedCacheName, FrequencySetContentHash.ToHex(contentHash));

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
        // The sets whose rows failed a load that this call waited for. Created at the first failure only, as this is the
        // hot path.
        HashSet<int> setsThatFailedALoad = null;

        while (true)
        {
            var source = await GetSource(setId);

            // Marks the entry most-recently-used. A load evicts the least-recently-used entries when it records the size of
            // its entry. See FrequencySetResidencyTracker's doc comment for why eviction has to be driven explicitly like this.
            residencyTracker.RecordAccess(source.LocalKey);

            try
            {
                return await GetOrLoadEntry(source);
            }
            catch (FrequencySetContentMismatchException exception)
            {
                // The rows that a load read for the content do not have its hash, so no caller gets them, and the set with
                // those rows now has a source of its own, which is not checked. With its source again, this caller loads its
                // own rows if it is that set, or else the content, from the rows of its own set. So the retries end: each
                // failed load quarantines one more set. A set that fails a second load of this call was not kept in
                // quarantine, or a caller had its source from just before the quarantine: this call then stops, and does
                // not read the database again and again.
                if (!(setsThatFailedALoad ??= []).Add(exception.SetId))
                {
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// The source of the set: the keys of its entry here, and of its value in the distributed cache. It comes from the row
    /// of the set, and this process reads the row again once each <c>ActiveSetCacheExpiryMinutes</c> while the set is in
    /// use: after a restore or a copy of the database, a set id can name another set, with other rows. A row with another
    /// content hash or import time, or no row, gives other keys, so the next call loads those frequencies. A set in use does
    /// not expire, so without the check, this process would use the old frequencies until it restarts.
    /// </summary>
    private async Task<FrequencySetSource> GetSource(int setId)
    {
        // One factory at a time for the key, as for the entry: one caller reads the row, and the others wait for it.
        return await cache.GetOrAddAsync(SetCheckCacheKey(setId), async () =>
            {
                var lastSource = cache.Get<FrequencySetSource>(LastSourceCacheKey(setId));

                Data.Models.HaplotypeFrequencySet set;
                if (lastSource == null)
                {
                    // The first read of the row: the frequencies of the set are not loaded, and their load needs the database
                    // anyway. So there is no timeout, and a fault fails the caller.
                    set = await frequencySetRepository.GetSet(setId);
                }
                else
                {
                    try
                    {
                        using var timeout = new CancellationTokenSource(SetCheckTimeout, timeProvider);
                        set = await frequencySetRepository.GetSet(setId, timeout.Token);
                    }
                    catch (Exception exception)
                    {
                        // A loaded set needs no database, so a fault or a timeout here does not fail the caller: the set keeps
                        // its source, and the next check is due an interval from now.
                        var reason = exception is OperationCanceledException ? $"no answer in {SetCheckTimeout.TotalSeconds} s" : exception.Message;
                        logger.SendTrace(
                            $"HF set {setId} could not be checked against its row in the database, so this process keeps it until the next check: {reason}",
                            LogLevel.Warn);
                        return lastSource;
                    }
                }

                var source = SourceOf(setId, set);

                if (lastSource != null && lastSource.RowVersion != source.RowVersion)
                {
                    logger.SendTrace($"HF set {setId} has another content hash or import time in the database, or no row, so this process loads it again.");
                }

                // No expiry: it is only replaced, by the next check.
                cache.Add(LastSourceCacheKey(setId), source, new MemoryCacheEntryOptions());
                return source;
            },
            SetCheckLocalEntryOptions()
        );
    }

    /// <param name="setId">The id of the set.</param>
    /// <param name="set">The row of the set; null when there is none.</param>
    private FrequencySetSource SourceOf(int setId, Data.Models.HaplotypeFrequencySet set)
    {
        if (set == null)
        {
            // There are no rows to read either: the database read gives the empty entry, as it did before the distributed
            // cache. So the distributed cache is not used.
            var missingSetKey = MissingSetCacheKey(setId);
            return new FrequencySetSource(setId, missingSetKey, DistributedKey: null, ContentHash: null, Active: false, RowVersion: "none", missingSetKey);
        }

        var rowVersion = $"{set.DateTimeAdded.UtcTicks}:{(set.ContentHash == null ? "no content hash" : FrequencySetContentHash.ToHex(set.ContentHash))}";
        var setKey = SetCacheKey(set);

        if (set.ContentHash == null)
        {
            return new FrequencySetSource(setId, setKey, FrequencySetDistributedCacheKey(set), ContentHash: null, set.Active, rowVersion, setKey);
        }

        var contentSource = new FrequencySetSource(
            setId,
            ContentCacheKey(set.ContentHash),
            ContentDistributedCacheKey(set.ContentHash),
            set.ContentHash,
            set.Active,
            rowVersion,
            setKey);

        // A quarantined set: its rows do not have the hash in its row, so it uses its own rows, and shares them with no other set.
        return IsQuarantined(set) ? contentSource.ForThisSetOnly() : contentSource;
    }

    private bool IsQuarantined(Data.Models.HaplotypeFrequencySet set) =>
        cache.Get<byte[]>(QuarantineCacheKey(set.Id)) is { } quarantinedHash && quarantinedHash.AsSpan().SequenceEqual(set.ContentHash);

    private async Task<FrequencySetCacheEntry> GetOrLoadEntry(FrequencySetSource source)
    {
        // GetOrAddAsync runs one factory at a time for the key, and concurrent callers wait for it. So when the distributed
        // cache misses or cannot be reached, this process reads each content, and each set with no content hash, from the
        // database once at a time: not once for each caller, and not once for each set with that content.
        return await cache.GetOrAddAsync(source.LocalKey, async cacheEntry =>
            {
                // An earlier entry under the key has left the cache, but its post-eviction callback can still wait on the
                // thread pool. Until RecordSize, the key holds no bytes, so the tracker cannot evict this entry while it
                // loads. It must not: no callback is registered on this entry yet, so its bytes would stay counted.
                residencyTracker.RecordLoadStart(source.LocalKey);

                var entry = await LoadEntry(source);

                var admission = residencyTracker.RecordSize(source.LocalKey, entry.EstimatedSizeInBytes);

                // Fires for every removal reason - expiry, or an explicit Remove, including the tracker's own onEvict →
                // cache.Remove call. In that last case this is a harmless no-op, since the tracker already removed the key
                // from its own bookkeeping before calling onEvict. Without this, an expiry would leave the tracker still
                // counting the key as resident - see FrequencySetResidencyTracker.Forget's doc comment. Registered here,
                // not in the options, so that it carries the admission of this entry.
                cacheEntry.RegisterPostEvictionCallback((_, _, _, _) => residencyTracker.Forget(source.LocalKey, admission));

                // The same entry instance is what gets warmed either way, so the pre-consolidation populates exactly
                // the object future callers will read. Exactly one writer in both modes - the branch decides only
                // whether that writer is on the critical path.
                if (cacheSettings.AwaitConsolidatedFrequencyWarm)
                {
                    // Inside the GetOrAddAsync factory, so the wait happens once per entry and every concurrent caller
                    // for the same entry awaits the same lazy task rather than racing it. When this returns,
                    // ConsolidatedFrequencies is populated and no caller can reach the direct-scan fallback, which is
                    // the whole point - a scan costs roughly a thousand times a warm read. A failure here propagates
                    // out of this factory instead of being swallowed, so GetOrAddAsync does not cache a half-warmed
                    // entry and the next caller for this entry retries from scratch.
                    WarmConsolidatedFrequencies(source, entry, rethrowOnFailure: true);
                }
                else
                {
                    // The right behaviour where a request is waiting: the full pre-consolidation is slow and must not
                    // delay the first lookup, which falls back to a direct calculation while ConsolidatedFrequencies
                    // is null.
                    _ = Task.Run(() => WarmConsolidatedFrequencies(source, entry, rethrowOnFailure: false));
                }

                return entry;
            },
            SetLocalEntryOptions()
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
    /// Loads the frequencies of a source from the distributed cache, or else from the database. Frequencies from the
    /// database are then stored in the distributed cache if the set is active.
    /// </summary>
    private async Task<FrequencySetCacheEntry> LoadEntry(FrequencySetSource source)
    {
        // A source with no key does not use the distributed cache, and with no Redis configured, there is nothing to read
        // from it and nothing to encode for it.
        var distributedCacheKey = source.DistributedKey is { } key && distributedCache.IsEnabled ? key : (DistributedCacheKey?)null;

        if (distributedCacheKey != null)
        {
            var storedEntry = await LoadEntryFromDistributedCache(source, distributedCacheKey.Value);
            if (storedEntry != null)
            {
                return storedEntry;
            }
        }

        var (rows, entry) = await LoadEntryFromDatabase(source.SetId);

        if (source.ContentHash != null)
        {
            EnsureTheRowsHaveTheContentHash(source, rows);
        }

        // Only an active set. An inactive set can be the set of an import that still runs, with only part of its rows:
        // stored, that part would reach every process that uses the set after its activation.
        if (distributedCacheKey != null && source.Active)
        {
            await StoreInDistributedCache(source, distributedCacheKey.Value, rows);
        }

        return entry;
    }

    /// <returns>The entry of the source in the distributed cache, or null on a miss.</returns>
    private async Task<FrequencySetCacheEntry> LoadEntryFromDistributedCache(FrequencySetSource source, DistributedCacheKey key)
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
                // A content is checked against its hash, and a set against its id.
                rows = source.ContentHash == null
                    ? FrequencySetPayload.Decode(source.SetId, payload)
                    : FrequencySetPayload.DecodeContent(source.ContentHash, payload);
            }
            catch (InvalidDataException exception)
            {
                // A miss: the frequencies are read from the database, and stored again.
                logger.SendTrace(
                    $"HF set {source.Description} in the distributed cache cannot be decoded, so it is read from the database: {exception.Message}",
                    LogLevel.Warn);
                return null;
            }

            return FrequencySetCacheEntry.Build(rows);
        }
    }

    /// <summary>
    /// Checks that the rows that a load read from the database for a content have its hash. They have it when the import
    /// or the backfill wrote the hash, as both calculate it from the rows in the database. They do not have it when, for
    /// example, the frequencies of the set were changed with SQL. Then no set must get these rows as the content: this logs
    /// an error, quarantines the set, and throws, so that the load of the shared entry fails, and each of its callers tries
    /// again with its own source. While its row has this hash, the set then uses its own rows only.
    /// </summary>
    /// <remarks>
    /// The check sees a wrong hash only when the set itself reads the content from the database. A hit in the distributed
    /// cache is not checked, and when another set with the hash loads it first, this set gets the rows of that set. So what
    /// makes the hash right is how the import and the backfill write it, not this check.
    /// </remarks>
    /// <exception cref="FrequencySetContentMismatchException">The rows do not have the content hash.</exception>
    private void EnsureTheRowsHaveTheContentHash(FrequencySetSource source, IReadOnlyList<LightweightHaplotypeFrequencyRecord> rows)
    {
        var hashOfTheRows = FrequencySetContentHash.Calculate(rows);
        if (hashOfTheRows.AsSpan().SequenceEqual(source.ContentHash))
        {
            return;
        }

        logger.SendTrace(
            $"HF set {source.SetId}: its rows do not have the content hash in its row ({FrequencySetContentHash.ToHex(source.ContentHash)}), " +
            $"but {FrequencySetContentHash.ToHex(hashOfTheRows)}. This process uses the rows of this set for this set only, until the hash in its row changes.",
            LogLevel.Error);

        // The quarantine has no expiry: it is replaced when the set is quarantined again, and a later check keeps the set
        // quarantined while its row has this hash. The source of the set only is its source at once, and also its last
        // source: a later check that cannot read the row keeps it.
        var sourceOfThisSetOnly = source.ForThisSetOnly();
        cache.Add(QuarantineCacheKey(source.SetId), source.ContentHash, new MemoryCacheEntryOptions());
        cache.Add(SetCheckCacheKey(source.SetId), sourceOfThisSetOnly, SetCheckLocalEntryOptions());
        cache.Add(LastSourceCacheKey(source.SetId), sourceOfThisSetOnly, new MemoryCacheEntryOptions());

        throw new FrequencySetContentMismatchException(source.SetId);
    }

    private async Task<(List<LightweightHaplotypeFrequencyRecord> Rows, FrequencySetCacheEntry Entry)> LoadEntryFromDatabase(int setId)
    {
        using (logger.RunTimed("Get All Frequencies from HF set - from SQL database", LogLevel.Verbose))
        {
            var rows = await frequencyRepository.GetAllHaplotypeFrequencies(setId);
            return (rows, FrequencySetCacheEntry.Build(rows));
        }
    }

    private async Task StoreInDistributedCache(FrequencySetSource source, DistributedCacheKey key, IReadOnlyList<LightweightHaplotypeFrequencyRecord> rows)
    {
        var payload = source.ContentHash == null
            ? FrequencySetPayload.Encode(source.SetId, rows)
            : FrequencySetPayload.EncodeContent(rows);

        // A value that is not stored is no fault here: the provider logged why, and the next process that misses it reads
        // the database again.
        var stored = await distributedCache.SetChunkedAsync(key, payload, FrequencySetDistributedCacheTimeToLive);

        logger.SendTrace(
            $"HF set {source.Description} for the distributed cache: {rows.Count} rows, {payload.Length} bytes in {ChunkedDistributedCacheExtensions.ChunkCount(payload.Length)} chunks."
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
    private void WarmConsolidatedFrequencies(FrequencySetSource source, FrequencySetCacheEntry entry, bool rethrowOnFailure)
    {
        try
        {
            // It is significantly faster to calculate all consolidated values up front than to calculate on the fly, even when caching individual values.
            // Many consolidated haplotypes may be inferable from the input data, but not actually represented in the haplotype frequency dataset.
            // Once for each entry: for a content, the set is the one whose call loaded it.
            using (logger.RunTimed($"Calculating consolidated frequencies with missing loci for set: {source.Description}"))
            {
                entry.ConsolidatedFrequencies = frequencyConsolidator.PreConsolidateFrequenciesForCommonMissingLoci(entry);
            }
        }
        catch (Exception e)
        {
            logger.SendTrace($"Failed to warm consolidated frequency cache for set {source.Description}: {e.Message}", LogLevel.Error);

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
