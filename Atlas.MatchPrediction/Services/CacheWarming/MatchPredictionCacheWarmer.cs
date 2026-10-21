using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;

namespace Atlas.MatchPrediction.Services.CacheWarming;

public interface IMatchPredictionCacheWarmer
{
    /// <summary>
    /// Loads the HLA Metadata Dictionary tables match prediction reads into memory, before the batch's donors run, for
    /// the nomenclature version of every active haplotype frequency set and for
    /// <paramref name="matchingAlgorithmHlaNomenclatureVersion"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call once per batch. After the first call of a process it costs a few cache reads: the tables stay cached until
    /// their cache entry expires (once a day), when the next call loads them again.
    /// </para>
    /// <para>
    /// Without it, each lookup that misses a cold cache is its own Table Storage read (ATL-476: about 240k of them on
    /// the small g group tables alone in one ATL-474 test run, repeated on every new replica).
    /// </para>
    /// <para>
    /// MACs are deliberately not warmed. Warming all of them (1.36 million on DEV) took 74-100 s on each new replica,
    /// raised worker memory by about 1.6 GB towards the replica's 4 GiB limit, and made live imputation no faster. A MAC
    /// is looked up when it is first seen and then kept for the life of the process.
    /// </para>
    /// <para>
    /// Best effort, never throws: if the warm-up fails, lookups still work, one row at a time, as before.
    /// </para>
    /// </remarks>
    /// <param name="matchingAlgorithmHlaNomenclatureVersion">
    /// The nomenclature version matching used for the search; null when the request has none.
    /// </param>
    Task WarmUp(string matchingAlgorithmHlaNomenclatureVersion);
}

internal class MatchPredictionCacheWarmer : IMatchPredictionCacheWarmer
{
    internal const string WarmedEventName = "Match prediction caches warmed";
    internal const string FailedEventName = "Match prediction cache warm-up failed";

    /// <summary>
    /// A warm-up shorter than this found everything already cached, so it is not logged: that is the normal case, once
    /// per batch.
    /// </summary>
    internal static readonly TimeSpan LoggedWarmUpThreshold = TimeSpan.FromSeconds(1);

    private readonly IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;
    private readonly IHaplotypeFrequencyCache haplotypeFrequencyCache;
    private readonly IAtlasLogger logger;

    public MatchPredictionCacheWarmer(
        IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory,
        IHaplotypeFrequencyCache haplotypeFrequencyCache,
        IAtlasLogger logger)
    {
        this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
        this.haplotypeFrequencyCache = haplotypeFrequencyCache;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task WarmUp(string matchingAlgorithmHlaNomenclatureVersion)
    {
        var stopwatch = Stopwatch.StartNew();
        IReadOnlyCollection<string> versions = [];

        try
        {
            versions = await HlaNomenclatureVersions(matchingAlgorithmHlaNomenclatureVersion);

            // Concurrently: on a cold process every one of these is a whole-table read, and the batch waits for all of them.
            await Task.WhenAll(versions.Select(version =>
                hlaMetadataDictionaryFactory.BuildCacheControl(version).PreWarmMatchPredictionCaches()));
        }
        catch (Exception exception)
        {
            LogFailure(exception, versions);
            return;
        }

        if (stopwatch.Elapsed >= LoggedWarmUpThreshold)
        {
            LogWarmUp(stopwatch.Elapsed, versions);
        }
    }

    /// <summary>
    /// The frequency sets' versions, because match prediction converts donor and patient typings in the frequency set's
    /// nomenclature, and the matching version, which it falls back to when a typing is not in the frequency set's version.
    /// </summary>
    private async Task<IReadOnlyCollection<string>> HlaNomenclatureVersions(string matchingAlgorithmHlaNomenclatureVersion)
    {
        var activeSets = await haplotypeFrequencyCache.GetActiveHaplotypeFrequencySets();

        return activeSets.Values
            .Select(set => set.HlaNomenclatureVersion)
            .Append(matchingAlgorithmHlaNomenclatureVersion)
            .Where(version => !string.IsNullOrEmpty(version))
            .Distinct()
            .Order()
            .ToList();
    }

    private void LogWarmUp(TimeSpan elapsed, IReadOnlyCollection<string> versions)
    {
        try
        {
            logger.SendEvent(
                WarmedEventName,
                LogLevel.Info,
                new Dictionary<string, string> { { "HlaNomenclatureVersions", string.Join(",", versions) } },
                new Dictionary<string, double> { { "WarmUpMs", elapsed.TotalMilliseconds } });
        }
        catch
        {
            // Logging must not fail the batch.
        }
    }

    private void LogFailure(Exception exception, IReadOnlyCollection<string> versions)
    {
        try
        {
            logger.SendException(exception, LogLevel.Warn, new Dictionary<string, string>
            {
                { "EventName", FailedEventName },
                { "HlaNomenclatureVersions", string.Join(",", versions) },
                { "VersionCount", versions.Count.ToString(CultureInfo.InvariantCulture) },
            });
        }
        catch
        {
            // Logging must not fail the batch.
        }
    }
}
