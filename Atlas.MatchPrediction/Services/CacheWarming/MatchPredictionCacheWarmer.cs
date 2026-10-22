using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;

namespace Atlas.MatchPrediction.Services.CacheWarming;

public interface IMatchPredictionCacheWarmer
{
    /// <summary>
    /// Loads the HLA Metadata Dictionary tables match prediction reads into memory, before the batch's donors run, for
    /// the nomenclature version of each haplotype frequency set the batch's patient and donors use, and for the
    /// nomenclature version matching used for the search.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call once per batch. After the first call of a process it costs a few cache reads: the tables stay cached until
    /// their cache entry expires (once a day), when the next call loads them again.
    /// </para>
    /// <para>
    /// Only the versions the batch uses, so a process loads the tables of the versions it needs, as it did before the
    /// warm-up existed.
    /// </para>
    /// <para>
    /// Without it, each lookup that misses a cold cache is its own Table Storage read, and every new replica repeats
    /// them.
    /// </para>
    /// <para>
    /// MACs are deliberately not warmed. The MAC table is much larger than the HLA tables: warming all of it made each
    /// new replica slow to start and used a large share of its memory, and it made live imputation no faster. A MAC is
    /// looked up when it is first seen and then kept for the life of the process.
    /// </para>
    /// <para>
    /// Best effort, never throws: if the warm-up fails, lookups still work, one row at a time, as before.
    /// </para>
    /// </remarks>
    Task WarmUp(MultipleDonorMatchProbabilityInput batch);
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
    private readonly IHaplotypeFrequencyLookupService haplotypeFrequencyLookupService;
    private readonly IAtlasLogger logger;

    public MatchPredictionCacheWarmer(
        IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory,
        IHaplotypeFrequencyLookupService haplotypeFrequencyLookupService,
        IAtlasLogger logger)
    {
        this.hlaMetadataDictionaryFactory = hlaMetadataDictionaryFactory;
        this.haplotypeFrequencyLookupService = haplotypeFrequencyLookupService;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task WarmUp(MultipleDonorMatchProbabilityInput batch)
    {
        var stopwatch = Stopwatch.StartNew();
        IReadOnlyCollection<string> versions = [];

        try
        {
            versions = await HlaNomenclatureVersions(batch);

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
    /// The versions of the frequency sets the batch uses, because match prediction converts donor and patient typings in
    /// the frequency set's nomenclature, and the matching version, which it falls back to when a typing is not in the
    /// frequency set's version.
    /// </summary>
    /// <remarks>
    /// The sets are chosen as they are for the patient and the donors, by
    /// <see cref="IHaplotypeFrequencyLookupService.GetSingleHaplotypeFrequencySet"/>: one cache read per distinct registry
    /// and ethnicity, not a database read.
    /// </remarks>
    private async Task<IReadOnlyCollection<string>> HlaNomenclatureVersions(MultipleDonorMatchProbabilityInput batch)
    {
        var setMetadata = (batch.Donors ?? [])
            .Select(donor => donor.DonorFrequencySetMetadata)
            .Append(batch.PatientFrequencySetMetadata)
            .Select(metadata => metadata ?? new FrequencySetMetadata())
            .DistinctBy(metadata => (metadata.RegistryCode, metadata.EthnicityCode));

        var versions = new List<string> { batch.MatchingAlgorithmHlaNomenclatureVersion };
        foreach (var metadata in setMetadata)
        {
            var set = await haplotypeFrequencyLookupService.GetSingleHaplotypeFrequencySet(metadata);
            versions.Add(set.HlaNomenclatureVersion);
        }

        return versions
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
