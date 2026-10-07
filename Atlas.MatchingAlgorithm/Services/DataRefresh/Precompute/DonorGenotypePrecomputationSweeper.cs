#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Exceptions;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Settings;
using Azure.Messaging.ServiceBus;
using EnumStringValues;
using Microsoft.Extensions.Logging;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;

/// <summary>
/// The sweeps over the runs and the batches of the donor genotype precomputation stage of the data refresh. The timers and
/// the dead-letter trigger of the data refresh function app run them.
/// </summary>
/// <remarks>
/// <para>
/// <b>The shape of the parallel match prediction timers</b> (<c>ParallelMatchPredictionAggregatorFunctions</c>). Every
/// move is a compare-and-swap in the repository, so two sweeps that overlap cannot both make one move, and a sweep that
/// fails is simply run again at the next tick. A failure is logged, and thrown when the sweep ends, so the invocation
/// shows as failed.
/// </para>
///
/// <para>
/// <b>A timer acts only on a running run of an open refresh record that has recreated its indexes.</b> Before that, no
/// run exists, and the dormant database can be scaled down and paused. A query of it every few minutes would keep it
/// awake, or fail while it resumes.
/// </para>
/// </remarks>
public interface IDonorGenotypePrecomputationSweeper
{
    /// <summary>
    /// Completes the run whose batches are all terminal: <see cref="DonorGenotypePrecomputationRunStatus.Completed"/>, or
    /// <see cref="DonorGenotypePrecomputationRunStatus.CompletedWithFailures"/>. The stage waits for this.
    /// </summary>
    Task FinaliseCompletedRuns();

    /// <summary>
    /// Moves the in-progress batches whose lease has expired to
    /// <see cref="DonorGenotypePrecomputationBatchStatus.Abandoned"/>, for the requeue sweep.
    /// </summary>
    Task MarkAbandonedBatches();

    /// <summary>
    /// Gives up on the failed and abandoned batches that have no retries left, sends the others back to
    /// <see cref="DonorGenotypePrecomputationBatchStatus.Pending"/>, and then publishes the batches that it sent back.
    /// </summary>
    /// <remarks>
    /// It publishes only the batches that a sweep sent back, which have a retry count above 0. The other pending batches
    /// are the stage's to send: a batch that a manual retry reset waits until the refresh continues and has scaled the
    /// database up again.
    /// </remarks>
    Task RequeueFailedBatches();

    /// <summary>
    /// Moves the batch of a dead-lettered message to <see cref="DonorGenotypePrecomputationBatchStatus.Abandoned"/>, so the
    /// requeue sweep sends it again or gives up on it. It reads the ids from the body of the message. The database is the
    /// one of the open refresh record that the message names. When that record is not open, it does nothing.
    /// </summary>
    Task AbandonDeadLetteredBatch(ServiceBusReceivedMessage message);
}

internal class DonorGenotypePrecomputationSweeper : IDonorGenotypePrecomputationSweeper
{
    private const string LoggingPrefix = "DONOR GENOTYPE PRECOMPUTATION:";

    /// <summary>
    /// The batches that one log message describes one by one. One message per sweep, not one per batch: telemetry is
    /// sampled, and a burst of messages, such as the failures of a database outage, would lose most of them.
    /// </summary>
    internal const int MaxDescribedBatchCount = 10;

    private readonly IDataRefreshHistoryRepository dataRefreshHistoryRepository;
    private readonly IStaticallyChosenDatabaseRepositoryFactory repositoryFactory;
    private readonly IDonorGenotypePrecomputationBatchDispatcher dispatcher;
    private readonly DonorGenotypePrecomputationSettings settings;
    private readonly ILogger<DonorGenotypePrecomputationSweeper> logger;

    public DonorGenotypePrecomputationSweeper(
        IDataRefreshHistoryRepository dataRefreshHistoryRepository,
        IStaticallyChosenDatabaseRepositoryFactory repositoryFactory,
        IDonorGenotypePrecomputationBatchDispatcher dispatcher,
        DonorGenotypePrecomputationSettings settings,
        ILogger<DonorGenotypePrecomputationSweeper> logger)
    {
        this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
        this.repositoryFactory = repositoryFactory;
        this.dispatcher = dispatcher;
        this.settings = settings;
        this.logger = logger;
    }

    /// <inheritdoc />
    public Task FinaliseCompletedRuns() => ForEachRunningRun(nameof(FinaliseCompletedRuns), FinaliseRun);

    /// <inheritdoc />
    public Task MarkAbandonedBatches() => ForEachRunningRun(nameof(MarkAbandonedBatches), AbandonExpiredBatches);

    /// <inheritdoc />
    public Task RequeueFailedBatches()
    {
        // Checked before any database is read: a sweep with a bad setting must not give up on batches.
        var maxBatchRetries = ValidatedMaxBatchRetries();
        return ForEachRunningRun(nameof(RequeueFailedBatches), (run, repository) => RequeueBatches(run, repository, maxBatchRetries));
    }

    /// <inheritdoc />
    public async Task AbandonDeadLetteredBatch(ServiceBusReceivedMessage message)
    {
        var request = DonorGenotypePrecomputationBatchRequest.FromBody(message.Body.ToString());
        if (request == null)
        {
            // Nothing can be done with the message, so it is completed. Its batch stays requested with no message, and
            // the stage waits for it until someone abandons the batch by hand.
            logger.LogError(
                LoggingPrefix + " A dead-lettered batch message names no batch, so no batch was abandoned. Message {MessageId}, " +
                "dead-letter reason: {DeadLetterReason}: {DeadLetterErrorDescription}",
                message.MessageId, message.DeadLetterReason, message.DeadLetterErrorDescription);
            return;
        }

        // Only the runs of open refreshes are swept, so the batch of a refresh that has ended needs nothing.
        var record = dataRefreshHistoryRepository.GetIncompleteRefreshJobs().SingleOrDefault(r => r.Id == request.DataRefreshRecordId);
        if (record == null)
        {
            logger.LogInformation(
                LoggingPrefix + " Batch {BatchId} of a dead-lettered message was left as it is: its data refresh record {DataRefreshRecordId} " +
                "is not open.",
                request.BatchId, request.DataRefreshRecordId);
            return;
        }

        var database = record.Database.ParseToEnum<TransientDatabase>();
        var reason = $"The message was dead-lettered. {message.DeadLetterReason}: {message.DeadLetterErrorDescription}";

        var repository = repositoryFactory.GetDonorGenotypePrecomputationRepositoryForDatabase(database);
        var wasAbandoned = await repository.TryMarkBatchAbandoned(request.DataRefreshRecordId, request.RunId, request.BatchId, reason);

        if (wasAbandoned)
        {
            logger.LogWarning(
                LoggingPrefix + " Batch {BatchId} of run {RunId} (data refresh record {DataRefreshRecordId}, {TargetDatabase}) was abandoned: " +
                "{Reason} The requeue sweep sends it again or gives up on it.",
                request.BatchId, request.RunId, request.DataRefreshRecordId, database, reason);
        }
        else
        {
            // A message of a run that has finished or was cancelled, or of a batch that is done already.
            logger.LogInformation(
                LoggingPrefix + " Batch {BatchId} of run {RunId} (data refresh record {DataRefreshRecordId}, {TargetDatabase}) of a " +
                "dead-lettered message was left as it is: its run is not running, or the batch waits for no message.",
                request.BatchId, request.RunId, request.DataRefreshRecordId, database);
        }
    }

    private async Task FinaliseRun(DonorGenotypePrecomputationRunLocation run, IDonorGenotypePrecomputationRepository repository)
    {
        var status = await repository.TryFinaliseRun(run.RunId);
        if (status == null)
        {
            return;
        }

        var counts = await repository.GetBatchCounts(run.RunId);
        logger.Log(
            status == DonorGenotypePrecomputationRunStatus.Completed ? LogLevel.Information : LogLevel.Warning,
            LoggingPrefix + " Run {RunId} (data refresh record {DataRefreshRecordId}, {TargetDatabase}) was finalised: {Status}. " +
            "{BatchCount} batch(es): {ResultsReceivedBatchCount} with results, {PermanentlyFailedBatchCount} permanently failed. " +
            "{FailedGroupCount} group(s) failed.",
            run.RunId,
            run.DataRefreshRecordId,
            run.TargetDatabase,
            status,
            counts.TotalBatchCount,
            counts.CountOf(DonorGenotypePrecomputationBatchStatus.ResultsReceived),
            counts.CountOf(DonorGenotypePrecomputationBatchStatus.PermanentlyFailed),
            counts.FailedGroupCount);
    }

    private async Task AbandonExpiredBatches(DonorGenotypePrecomputationRunLocation run, IDonorGenotypePrecomputationRepository repository)
    {
        var abandoned = await repository.MarkExpiredBatchesAbandoned(run.RunId);
        if (abandoned.Count == 0)
        {
            return;
        }

        logger.LogWarning(
            LoggingPrefix + " The lease of {BatchCount} batch(es) of run {RunId} (data refresh record {DataRefreshRecordId}, {TargetDatabase}) " +
            "expired: {BatchIds}. The requeue sweep sends them again or gives up on them.",
            abandoned.Count, run.RunId, run.DataRefreshRecordId, run.TargetDatabase, DescribeIds(abandoned));
    }

    private async Task RequeueBatches(DonorGenotypePrecomputationRunLocation run, IDonorGenotypePrecomputationRepository repository, int maxBatchRetries)
    {
        // The retry count of a swept batch is its count before the move: the requeues so far. So a batch has made one
        // attempt more than its retry count, and a requeued batch waits for its attempt two above it.
        var givenUp = await repository.MarkBatchesPermanentlyFailed(run.RunId, maxBatchRetries);
        if (givenUp.Count > 0)
        {
            logger.LogError(
                LoggingPrefix + " {BatchCount} batch(es) of run {RunId} (data refresh record {DataRefreshRecordId}, {TargetDatabase}) " +
                "permanently failed: {Batches}",
                givenUp.Count, run.RunId, run.DataRefreshRecordId, run.TargetDatabase,
                DescribeBatches(givenUp, batch => $"{batch.RetryCount + 1} attempt(s)"));
        }

        var requeued = await repository.RequeueRetryableBatches(run.RunId, maxBatchRetries);
        if (requeued.Count > 0)
        {
            logger.LogWarning(
                LoggingPrefix + " {BatchCount} batch(es) of run {RunId} (data refresh record {DataRefreshRecordId}, {TargetDatabase}) " +
                "were requeued: {Batches}",
                requeued.Count, run.RunId, run.DataRefreshRecordId, run.TargetDatabase,
                DescribeBatches(requeued, batch => $"next attempt {batch.RetryCount + 2}"));
        }

        // Also when this sweep sent nothing back: a sweep that stopped before it published leaves its batches pending.
        await dispatcher.DispatchPendingBatches(run, PendingBatchSelection.Requeued);
    }

    /// <summary>
    /// Runs <paramref name="sweep"/> on the running run of each open refresh record that has recreated its indexes. A
    /// failure of one record does not stop the others; they are thrown together at the end.
    /// </summary>
    private async Task ForEachRunningRun(
        string sweepName,
        Func<DonorGenotypePrecomputationRunLocation, IDonorGenotypePrecomputationRepository, Task> sweep)
    {
        // Before the indexes are recreated, the stage has not started, and the database can be scaled down.
        var records = dataRefreshHistoryRepository.GetIncompleteRefreshJobs()
            .Where(record => record.IsStageComplete(DataRefreshStage.IndexRecreation))
            .ToList();

        var failures = new List<Exception>();
        foreach (var record in records)
        {
            try
            {
                var database = record.Database.ParseToEnum<TransientDatabase>();
                var repository = repositoryFactory.GetDonorGenotypePrecomputationRepositoryForDatabase(database);

                var run = await repository.GetRun(record.Id);
                if (run?.Status != DonorGenotypePrecomputationRunStatus.Running)
                {
                    continue;
                }

                await sweep(new DonorGenotypePrecomputationRunLocation(record.Id, database, run.Id), repository);
            }
            catch (Exception e)
            {
                // Every move is a compare-and-swap, so the next tick simply tries again.
                logger.LogError(e, LoggingPrefix + " {SweepName} failed for data refresh record {DataRefreshRecordId}.", sweepName, record.Id);
                failures.Add(new InvalidOperationException($"{sweepName} failed for data refresh record {record.Id}.", e));
            }
        }

        if (failures.Count > 0)
        {
            throw new AggregateException($"{sweepName} failed for {failures.Count} of {records.Count} data refresh record(s).", failures);
        }
    }

    private int ValidatedMaxBatchRetries()
    {
        if (settings.MaxBatchRetries < 0)
        {
            throw new InvalidDataRefreshConfigurationException(
                $"{nameof(DonorGenotypePrecomputationSettings.MaxBatchRetries)} is {settings.MaxBatchRetries}. It must be 0 or more.");
        }

        return settings.MaxBatchRetries;
    }

    private static string DescribeBatches(
        IReadOnlyCollection<SweptDonorGenotypePrecomputationBatch> batches,
        Func<SweptDonorGenotypePrecomputationBatch, string> describeAttempts) =>
        Describe(batches, batch => $"batch {batch.BatchId}, {describeAttempts(batch)}, was {batch.PreviousStatus}: {batch.FailureMessage}");

    private static string DescribeIds(IReadOnlyCollection<SweptDonorGenotypePrecomputationBatch> batches) =>
        Describe(batches, batch => batch.BatchId.ToString());

    private static string Describe(
        IReadOnlyCollection<SweptDonorGenotypePrecomputationBatch> batches,
        Func<SweptDonorGenotypePrecomputationBatch, string> describe)
    {
        var described = string.Join("; ", batches.Take(MaxDescribedBatchCount).Select(describe));
        return batches.Count > MaxDescribedBatchCount ? $"{described}; and {batches.Count - MaxDescribedBatchCount} more" : described;
    }
}
