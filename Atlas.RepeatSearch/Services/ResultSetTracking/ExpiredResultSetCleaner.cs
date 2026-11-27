using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.RepeatSearch.Data.Repositories;
using Atlas.RepeatSearch.Settings;

namespace Atlas.RepeatSearch.Services.ResultSetTracking;

public interface IExpiredResultSetCleaner
{
    /// <summary>
    /// Deletes the result sets of original searches that were last used before the retention period, one set at a time: the donor codes in
    /// batches, then the set row. Stops at the time limit of a run, or when no expired set is left. A set that a run does not finish stays
    /// marked as being deleted, and the next run continues it first.
    /// </summary>
    Task DeleteExpiredResultSets(CancellationToken cancellationToken);
}

internal class ExpiredResultSetCleaner : IExpiredResultSetCleaner
{
    /// <summary>
    /// Kept out of sampling by <see cref="RepeatSearchEventsTelemetryInitializer"/>.
    /// </summary>
    internal const string DeletedEventName = "Expired repeat search result sets deleted";

    private readonly ICanonicalResultSetRepository canonicalResultSetRepository;
    private readonly TimeProvider timeProvider;
    private readonly IAtlasLogger logger;
    private readonly int retentionDays;
    private readonly int batchSize;
    private readonly TimeSpan maxRunTime;

    public ExpiredResultSetCleaner(
        ICanonicalResultSetRepository canonicalResultSetRepository,
        RepeatSearchSettings settings,
        TimeProvider timeProvider,
        IAtlasLogger logger)
    {
        this.canonicalResultSetRepository = canonicalResultSetRepository;
        this.timeProvider = timeProvider;
        this.logger = logger;
        retentionDays = settings.ResultsRetentionDays;
        batchSize = settings.ResultsDeleteBatchSize;
        maxRunTime = TimeSpan.FromMinutes(settings.ResultsDeleteMaxRunMinutes);
    }

    public async Task DeleteExpiredResultSets(CancellationToken cancellationToken)
    {
        if (retentionDays <= 0)
        {
            logger.SendTrace(
                $"The retention of repeat search result sets is off: {nameof(RepeatSearchSettings.ResultsRetentionDays)} is {retentionDays}.");
            return;
        }

        // Checked before a set is claimed: with no rows in a batch, the job would never finish the set, and each run would take it again.
        if (batchSize < 1)
        {
            logger.SendTrace(
                $"The retention job of repeat search result sets does not run: {nameof(RepeatSearchSettings.ResultsDeleteBatchSize)} is " +
                $"{batchSize}, and it must be 1 or more.",
                LogLevel.Warn);
            return;
        }

        var startTime = timeProvider.GetUtcNow();
        var cutOff = startTime.AddDays(-retentionDays);
        var deadline = startTime + maxRunTime;

        var setCount = 0;
        long rowCount = 0;
        var result = RunResult.Failed;
        int? setId = null;

        // The event is sent also when the run fails or is cancelled, so that the rows it deleted are reported.
        try
        {
            // One batch in each pass, so that the time limit is checked before each batch.
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (timeProvider.GetUtcNow() >= deadline)
                {
                    result = RunResult.TimeLimit;
                    break;
                }

                setId ??= await canonicalResultSetRepository.StartDeletingExpiredResultSet(cutOff, cancellationToken);
                if (setId == null)
                {
                    result = RunResult.Completed;
                    break;
                }

                var deletedRows = await canonicalResultSetRepository.DeleteResultsBatch(setId.Value, batchSize, cancellationToken);
                rowCount += deletedRows;

                // Nothing adds rows to a set that is being deleted, so a short batch was the last one.
                if (deletedRows < batchSize)
                {
                    await canonicalResultSetRepository.DeleteResultSet(setId.Value, cancellationToken);
                    setCount++;
                    setId = null;
                }
            }
        }
        // The type of the exception of a cancelled SQL call depends on when the cancel comes, so the token tells why the run stopped.
        // Each SQL call is one statement, which commits or rolls back as a whole, so the next run continues the set.
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            result = RunResult.Cancelled;
            throw;
        }
        finally
        {
            logger.SendEvent(DeletedEventName,
                props: new Dictionary<string, string>
                {
                    {"RetentionDays", retentionDays.ToString(CultureInfo.InvariantCulture)},
                    {"CutOff", cutOff.ToString("O", CultureInfo.InvariantCulture)},
                    {"Result", result.ToString()}
                },
                metrics: new Dictionary<string, double>
                {
                    {"SetCount", setCount},
                    {"RowCount", rowCount},
                    {"ElapsedMs", (timeProvider.GetUtcNow() - startTime).TotalMilliseconds}
                });
        }
    }

    /// <summary>
    /// How a run ended. <see cref="Failed"/> is the value while the run works, so it is reported when an exception stops the run.
    /// </summary>
    private enum RunResult
    {
        Failed,
        Completed,
        TimeLimit,
        Cancelled
    }
}
