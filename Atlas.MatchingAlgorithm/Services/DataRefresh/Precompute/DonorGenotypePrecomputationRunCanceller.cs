#nullable enable

using System;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using EnumStringValues;
using Microsoft.Extensions.Logging;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;

/// <summary>
/// Stops the donor genotype precomputation run of a data refresh that failed, and removes the staging data of a data refresh
/// that ended.
/// </summary>
public interface IDonorGenotypePrecomputationRunCanceller
{
    /// <summary>
    /// Removes the staging data of the database of the record. Call it for a record that is closed as successful: its
    /// database is the active one now. The run stays.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Normally the stage removed the staging data already. After a manual Complete decision, the stage does not run again,
    /// so its staging data stays until this removes it.
    /// </para>
    ///
    /// <para>
    /// <b>Does not throw.</b> The record is closed and its notifications are sent already, so an error here is only logged.
    /// The next refresh of the database removes the staging data.
    /// </para>
    /// </remarks>
    Task RemoveStagingData(DataRefreshRecord refreshRecord);

    /// <summary>
    /// Cancels the run of the record, if it is building or running, and then removes the staging data of the database of
    /// the record. Call it only for a record that is closed as failed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The record is closed before this call, so the workers already skip its messages, and the timers already leave it.
    /// The cancel also stops a worker that read the record before it closed, because the claim takes a batch only from a
    /// running run. And the status of the run then shows that the work stopped. A failed refresh does not continue, so no
    /// later stage uses its run or its staging data.
    /// </para>
    ///
    /// <para>
    /// <b>Does not throw.</b> The record is closed and the failure is reported already, so an error here is only logged.
    /// The next refresh removes all the precomputation data of the database before it builds a new run.
    /// </para>
    /// </remarks>
    Task CancelRun(DataRefreshRecord refreshRecord);
}

/// <inheritdoc />
internal class DonorGenotypePrecomputationRunCanceller : IDonorGenotypePrecomputationRunCanceller
{
    private const string LoggingPrefix = "DONOR GENOTYPE PRECOMPUTATION:";

    private readonly IStaticallyChosenDatabaseRepositoryFactory repositoryFactory;
    private readonly ILogger<DonorGenotypePrecomputationRunCanceller> logger;

    public DonorGenotypePrecomputationRunCanceller(
        IStaticallyChosenDatabaseRepositoryFactory repositoryFactory,
        ILogger<DonorGenotypePrecomputationRunCanceller> logger)
    {
        this.repositoryFactory = repositoryFactory;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task RemoveStagingData(DataRefreshRecord refreshRecord)
    {
        var recordId = refreshRecord.Id;
        var recordDatabase = refreshRecord.Database;

        try
        {
            var database = recordDatabase.ParseToEnum<TransientDatabase>();
            var repository = repositoryFactory.GetDonorGenotypePrecomputationRepositoryForDatabase(database);
            await repository.TruncateStagingTables();

            logger.LogInformation(
                LoggingPrefix + " The staging data of data refresh record {DataRefreshRecordId} ({TargetDatabase}) was removed after the swap.",
                recordId, database);
        }
        catch (Exception e)
        {
            logger.LogError(
                e,
                LoggingPrefix + " The staging data of data refresh record {DataRefreshRecordId} ({TargetDatabase}) could not be removed " +
                "after the swap. The next refresh of the database removes it.",
                recordId, recordDatabase);
        }
    }

    /// <inheritdoc />
    public async Task CancelRun(DataRefreshRecord refreshRecord)
    {
        var recordId = refreshRecord.Id;
        var recordDatabase = refreshRecord.Database;

        try
        {
            // The database of the record, not the dormant one: the workers and the timers also use the record.
            var database = recordDatabase.ParseToEnum<TransientDatabase>();
            var repository = repositoryFactory.GetDonorGenotypePrecomputationRepositoryForDatabase(database);

            var wasCancelled = await repository.TryMarkRunCancelled(recordId);

            // Also when no run was cancelled: a refresh that failed in the report of a complete run still has its staging data.
            await repository.TruncateStagingTables();

            if (wasCancelled)
            {
                logger.LogWarning(
                    LoggingPrefix + " The run of data refresh record {DataRefreshRecordId} ({TargetDatabase}) was cancelled, because the " +
                    "refresh failed. The workers skip its messages. Its staging data was removed.",
                    recordId, database);
            }
            else
            {
                logger.LogInformation(
                    LoggingPrefix + " Data refresh record {DataRefreshRecordId} ({TargetDatabase}) failed with no run to cancel. The " +
                    "staging data was removed.",
                    recordId, database);
            }
        }
        catch (Exception e)
        {
            logger.LogError(
                e,
                LoggingPrefix + " The run of data refresh record {DataRefreshRecordId} ({TargetDatabase}) could not be cancelled, or its " +
                "staging data could not be removed. The workers skip the messages of the closed record: only the batches that they " +
                "took before it closed can still finish. The next refresh removes the run and its staging data.",
                recordId, recordDatabase);
        }
    }
}
