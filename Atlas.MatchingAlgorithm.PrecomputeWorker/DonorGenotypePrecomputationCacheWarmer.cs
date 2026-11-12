using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchPrediction.Services.CacheWarming;

namespace Atlas.MatchingAlgorithm.PrecomputeWorker;

/// <summary>
/// Loads the HLA Metadata Dictionary tables that imputation reads into memory, before the worker takes its first message.
/// </summary>
internal interface IDonorGenotypePrecomputationCacheWarmer
{
    /// <summary>
    /// Warms the tables for the HLA nomenclature version of each open data refresh record, and for the version of each
    /// active haplotype frequency set. Warms nothing when no open record has a version.
    /// </summary>
    /// <remarks>Best effort: it never throws. A failure is logged, and the worker starts with cold tables.</remarks>
    Task WarmUp();
}

/// <inheritdoc />
/// <remarks>
/// <para>
/// <b>Why.</b> Until a table is in the cache, each lookup in it is a single-row Table Storage read, while the whole table
/// loads in the background. A new replica takes its first batches at once, so without the warm-up it makes thousands of
/// these reads before its tables are in memory.
/// </para>
///
/// <para>
/// <b>Which versions.</b> A batch converts to P groups at the version of its data refresh record, and imputes with the
/// frequency set of the registry and ethnicity of each group. The worker does not know its batches before it takes them,
/// so it warms the version of each open record and of each active set. A set that is imported during the stage, at a
/// version that no active set had, is not warmed: its lookups read single rows until its tables are loaded.
/// </para>
///
/// <para>
/// <b>No open record</b>, for example when the worker reads the messages of a refresh that has ended: it warms nothing,
/// because the worker skips every message.
/// </para>
/// </remarks>
internal class DonorGenotypePrecomputationCacheWarmer(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<DonorGenotypePrecomputationCacheWarmer> logger) : IDonorGenotypePrecomputationCacheWarmer
{
    public async Task WarmUp()
    {
        try
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();

            var versions = await scope.ServiceProvider.GetRequiredService<IDataRefreshHistoryRepository>()
                .GetIncompleteRefreshJobHlaNomenclatureVersions();
            if (versions.Count == 0)
            {
                logger.LogInformation(
                    "No open data refresh record has an HLA nomenclature version, so the HLA Metadata Dictionary caches are not warmed.");
                return;
            }

            logger.LogInformation(
                "Warming the HLA Metadata Dictionary caches for HLA nomenclature version(s) {HlaNomenclatureVersions} " +
                "and the active haplotype frequency sets.",
                string.Join(",", versions));

            // It logs its own failures, and does not throw.
            await scope.ServiceProvider.GetRequiredService<IMatchPredictionCacheWarmer>().WarmUpForActiveFrequencySets(versions);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception,
                "The HLA Metadata Dictionary caches could not be warmed. " +
                "Until the tables are loaded, each lookup reads a single row from Table Storage.");
        }
    }
}
