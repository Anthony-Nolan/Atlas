#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Exceptions;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Notifications;
using Atlas.MatchingAlgorithm.Settings;
using EnumStringValues;
using Microsoft.Extensions.Logging;
using BatchStatus = Atlas.MatchingAlgorithm.Data.Models.Entities.DonorGenotypePrecomputationBatchStatus;
using Run = Atlas.MatchingAlgorithm.Data.Models.Entities.DonorGenotypePrecomputationRun;
using RunStatus = Atlas.MatchingAlgorithm.Data.Models.Entities.DonorGenotypePrecomputationRunStatus;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;

/// <summary>
/// The donor genotype precomputation stage of the data refresh: it builds the work of a run, sends it to the precomputation
/// workers, waits until the run is complete, and reports the failures of the run.
/// </summary>
public interface IDonorGenotypePrecomputationStage
{
    /// <summary>Runs the stage for the refresh record. Returns when the run is complete and its failures are reported.</summary>
    /// <remarks>
    /// <para>
    /// <b>The run is the state of the stage.</b> The stage reads the run of the record and continues from there, whatever
    /// <paramref name="executionMode"/> is: it builds a run that has no build yet, sends the pending batches of a running
    /// run, and waits for it. So a stage that stopped part way, and a run that a manual retry sent back to running,
    /// continue with no second build.
    /// </para>
    ///
    /// <para>
    /// <b>The stage waits in this invocation.</b> The workers compute the batches, and the timers of the data refresh app
    /// complete the run when every batch is done. The stage reads the run until then.
    /// </para>
    /// </remarks>
    /// <param name="refreshRecord">The record of the refresh. It must name the dormant database.</param>
    /// <param name="executionMode">Only logged.</param>
    /// <param name="cancellationToken">
    /// Cancelled when this invocation loses its lease on the refresh record: the stage stops, and the workers go on.
    /// </param>
    /// <exception cref="InvalidDataRefreshConfigurationException">A setting of the stage is out of range.</exception>
    /// <exception cref="InvalidOperationException">
    /// The record does not name the dormant database, or the run of the record was cancelled.
    /// </exception>
    Task Run(DataRefreshRecord refreshRecord, DataRefreshStageExecutionMode executionMode, CancellationToken cancellationToken);
}

/// <inheritdoc />
internal class DonorGenotypePrecomputationStage : IDonorGenotypePrecomputationStage
{
    private const string LoggingPrefix = "DONOR GENOTYPE PRECOMPUTATION:";

    private readonly IDormantRepositoryFactory dormantRepositoryFactory;
    private readonly IActiveDatabaseProvider activeDatabaseProvider;
    private readonly IDonorGenotypePrecomputationBatchDispatcher dispatcher;
    private readonly IDataRefreshSupportNotificationSender notificationSender;
    private readonly DonorGenotypePrecomputationSettings settings;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<DonorGenotypePrecomputationStage> logger;

    public DonorGenotypePrecomputationStage(
        IDormantRepositoryFactory dormantRepositoryFactory,
        IActiveDatabaseProvider activeDatabaseProvider,
        IDonorGenotypePrecomputationBatchDispatcher dispatcher,
        IDataRefreshSupportNotificationSender notificationSender,
        DonorGenotypePrecomputationSettings settings,
        TimeProvider timeProvider,
        ILogger<DonorGenotypePrecomputationStage> logger)
    {
        this.dormantRepositoryFactory = dormantRepositoryFactory;
        this.activeDatabaseProvider = activeDatabaseProvider;
        this.dispatcher = dispatcher;
        this.notificationSender = notificationSender;
        this.settings = settings;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task Run(DataRefreshRecord refreshRecord, DataRefreshStageExecutionMode executionMode, CancellationToken cancellationToken)
    {
        // Both checks come before any database call: a bad setting or a wrong database must not show only after hours of work.
        ValidateSettings();
        var database = TargetDatabaseOf(refreshRecord);
        var repository = dormantRepositoryFactory.GetDonorGenotypePrecomputationRepository();

        var run = await repository.GetRun(refreshRecord.Id);
        logger.LogInformation(
            LoggingPrefix + " The stage starts for data refresh record {DataRefreshRecordId} ({TargetDatabase}) as {ExecutionMode}. " +
            "Run {RunId} is {RunStatus}.",
            refreshRecord.Id, database, executionMode, run?.Id, run?.Status);

        if (run is null or { Status: RunStatus.Building })
        {
            run = await Build(refreshRecord, database, repository, cancellationToken);
        }

        if (run.Status == RunStatus.Cancelled)
        {
            throw new InvalidOperationException(
                $"Donor genotype precomputation run {run.Id} of data refresh record {refreshRecord.Id} was cancelled, because the refresh " +
                "failed. Request a new data refresh.");
        }

        if (run.Status == RunStatus.Running)
        {
            await dispatcher.DispatchPendingBatches(
                new DonorGenotypePrecomputationRunLocation(refreshRecord.Id, database, run.Id),
                PendingBatchSelection.All);
            run = await WaitUntilTheRunIsComplete(refreshRecord, database, run.Id, repository, cancellationToken);
        }

        await ReportTheFailures(refreshRecord, database, run, repository);
    }

    private void ValidateSettings()
    {
        if (settings.GroupsPerBatch < 1
            || settings.PollIntervalSeconds < 1
            || settings.StallAlertMinutes < 1
            || settings.MaxFailedDonorFraction is not (>= 0 and <= 1))
        {
            throw new InvalidDataRefreshConfigurationException(
                $"The donor genotype precomputation settings are invalid. {nameof(settings.GroupsPerBatch)} ({settings.GroupsPerBatch}), " +
                $"{nameof(settings.PollIntervalSeconds)} ({settings.PollIntervalSeconds}) and {nameof(settings.StallAlertMinutes)} " +
                $"({settings.StallAlertMinutes}) must be 1 or more, and {nameof(settings.MaxFailedDonorFraction)} " +
                $"({settings.MaxFailedDonorFraction}) must be from 0 to 1.");
        }
    }

    /// <summary>
    /// The database that the stage builds in: the dormant one, where the earlier stages wrote the donors. The workers, the
    /// timers and the dead-letter trigger take the database from the record. The requester writes the dormant database on
    /// the record, so the two are the same. If they were not, the stage would build where no worker looks.
    /// </summary>
    private TransientDatabase TargetDatabaseOf(DataRefreshRecord refreshRecord)
    {
        var dormantDatabase = activeDatabaseProvider.GetDormantDatabase();
        var recordDatabase = refreshRecord.Database.ParseToEnum<TransientDatabase>();
        if (recordDatabase != dormantDatabase)
        {
            throw new InvalidOperationException(
                $"Data refresh record {refreshRecord.Id} names {recordDatabase}, but the dormant database is {dormantDatabase}. The donor " +
                "genotype precomputation workers and timers use the database of the record, so the stage cannot run.");
        }

        return dormantDatabase;
    }

    private async Task<Run> Build(
        DataRefreshRecord refreshRecord,
        TransientDatabase database,
        IDonorGenotypePrecomputationRepository repository,
        CancellationToken cancellationToken)
    {
        var run = await repository.StartBuild(refreshRecord.Id, refreshRecord.HlaNomenclatureVersion, settings.GroupsPerBatch);
        logger.LogInformation(
            LoggingPrefix + " Run {RunId} of data refresh record {DataRefreshRecordId} ({TargetDatabase}) is being built, with " +
            "{GroupsPerBatch} groups per batch.",
            run.Id, refreshRecord.Id, database, run.GroupsPerBatch);

        var result = await repository.BuildRun(run.Id, cancellationToken);
        logger.LogInformation(
            LoggingPrefix + " Run {RunId} of data refresh record {DataRefreshRecordId} ({TargetDatabase}) is built: {RunStatus}. " +
            "{DonorCount} donors, {GroupCount} groups, {BatchCount} batches, {DonorAssignmentCount} donor rows. Steps: {BuildSteps}",
            run.Id, refreshRecord.Id, database, result.Status, result.TotalDonorCount, result.TotalGroupCount, result.TotalBatchCount,
            result.TotalDonorAssignmentCount, DescribeSteps(result.Steps));

        return await repository.GetRun(refreshRecord.Id)
               ?? throw new InvalidOperationException($"Run {run.Id} of data refresh record {refreshRecord.Id} is gone after its build.");
    }

    /// <summary>
    /// Reads the run every <see cref="DonorGenotypePrecomputationSettings.PollIntervalSeconds"/>, until the finaliser has
    /// completed it. Sends one stall alert when no batch finishes for
    /// <see cref="DonorGenotypePrecomputationSettings.StallAlertMinutes"/>, and arms it again when a batch finishes.
    /// </summary>
    /// <remarks>
    /// A poll that fails with a temporary error (see <see cref="PrecomputeErrorClassifier"/>) does not stop the wait: the
    /// stage reads again at the next poll. The wait lasts many hours, and an error that stops the stage also scales the
    /// database down while the workers write to it, until the next attempt scales it up again. When the polls fail for
    /// <see cref="DonorGenotypePrecomputationSettings.StallAlertMinutes"/>, the error is not temporary, and the stage throws it.
    /// </remarks>
    private async Task<Run> WaitUntilTheRunIsComplete(
        DataRefreshRecord refreshRecord,
        TransientDatabase database,
        int runId,
        IDonorGenotypePrecomputationRepository repository,
        CancellationToken cancellationToken)
    {
        var pollInterval = TimeSpan.FromSeconds(settings.PollIntervalSeconds);
        var stallAlertTime = TimeSpan.FromMinutes(settings.StallAlertMinutes);
        var stallWatch = new StallWatch(stallAlertTime, timeProvider.GetUtcNow());
        DateTimeOffset? firstFailedPollTime = null;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Run run;
            DonorGenotypePrecomputationBatchCounts? counts;
            try
            {
                (run, counts) = await PollTheRun(refreshRecord.Id, runId, repository);
            }
            catch (Exception e) when (PrecomputeErrorClassifier.Classify(e) == PrecomputeErrorKind.KnownTemporary)
            {
                var failedPollTime = timeProvider.GetUtcNow();
                firstFailedPollTime ??= failedPollTime;
                var timeWithFailedPolls = failedPollTime - firstFailedPollTime.Value;
                if (timeWithFailedPolls >= stallAlertTime)
                {
                    throw;
                }

                logger.LogWarning(
                    e,
                    LoggingPrefix + " The stage could not read run {RunId} of data refresh record {DataRefreshRecordId} ({TargetDatabase}) " +
                    "because of a temporary error. The polls have failed for {MinutesWithFailedPolls:F0} minutes. The stage reads the run " +
                    "again at the next poll.",
                    runId, refreshRecord.Id, database, timeWithFailedPolls.TotalMinutes);
                await Task.Delay(pollInterval, timeProvider, cancellationToken);
                continue;
            }

            if (counts == null)
            {
                return run;
            }

            firstFailedPollTime = null;
            await ObserveProgress(refreshRecord, database, run.Id, counts, stallWatch);
            LogProgress(refreshRecord, database, run.Id, counts);
            await Task.Delay(pollInterval, timeProvider, cancellationToken);
        }
    }

    /// <summary>
    /// One poll of the wait: the run, and its batch counts while it is running. The counts are null when the run is complete.
    /// </summary>
    /// <exception cref="InvalidOperationException">The run is gone, or it is neither running nor complete.</exception>
    private static async Task<(Run Run, DonorGenotypePrecomputationBatchCounts? Counts)> PollTheRun(
        int dataRefreshRecordId,
        int runId,
        IDonorGenotypePrecomputationRepository repository)
    {
        var run = await repository.GetRun(dataRefreshRecordId)
                  ?? throw new InvalidOperationException($"Run {runId} of data refresh record {dataRefreshRecordId} is gone during the wait.");

        return run.Status switch
        {
            RunStatus.Completed or RunStatus.CompletedWithFailures => (run, null),
            RunStatus.Running => (run, await repository.GetBatchCounts(run.Id)),
            _ => throw new InvalidOperationException(
                $"Run {run.Id} of data refresh record {dataRefreshRecordId} became {run.Status} while the stage waited for it.")
        };
    }

    /// <summary>Sends the stall alert when a stall starts, and logs when the stall ends.</summary>
    private async Task ObserveProgress(
        DataRefreshRecord refreshRecord,
        TransientDatabase database,
        int runId,
        DonorGenotypePrecomputationBatchCounts counts,
        StallWatch stallWatch)
    {
        var now = timeProvider.GetUtcNow();
        switch (stallWatch.Observe(counts.TerminalBatchCount, now))
        {
            case StallWatch.Observation.StallStarted:
                var timeWithoutProgress = stallWatch.TimeWithoutProgress(now);
                logger.LogWarning(
                    LoggingPrefix + " No batch of run {RunId} of data refresh record {DataRefreshRecordId} ({TargetDatabase}) has " +
                    "finished for {MinutesWithoutProgress:F0} minutes. A stall alert is sent.",
                    runId, refreshRecord.Id, database, timeWithoutProgress.TotalMinutes);
                await notificationSender.SendPrecomputationStallAlert(refreshRecord.Id, runId, timeWithoutProgress, counts);
                break;
            case StallWatch.Observation.StallEnded:
                logger.LogInformation(
                    LoggingPrefix + " Batches of run {RunId} of data refresh record {DataRefreshRecordId} ({TargetDatabase}) finish again.",
                    runId, refreshRecord.Id, database);
                break;
        }
    }

    /// <summary>
    /// Sends the failures of the run, if it has any, then removes the staging data. The data refresh goes on in all cases.
    /// </summary>
    /// <remarks>
    /// The alert comes before the removal: a stop between the two sends the alert again, and never loses it. The removal is
    /// the last step, so a run with no staging data was reported before, and is not reported again. A run with no donors
    /// has no staging data either.
    /// </remarks>
    private async Task ReportTheFailures(
        DataRefreshRecord refreshRecord,
        TransientDatabase database,
        Run run,
        IDonorGenotypePrecomputationRepository repository)
    {
        if (!await repository.HasStagingData())
        {
            logger.LogInformation(
                LoggingPrefix + " Run {RunId} of data refresh record {DataRefreshRecordId} ({TargetDatabase}) is {RunStatus}, and has no " +
                "staging data to report. The stage is complete.",
                run.Id, refreshRecord.Id, database, run.Status);
            return;
        }

        if (run.Status == RunStatus.CompletedWithFailures)
        {
            var summary = await repository.GetFailureSummary(run.Id)
                          ?? throw new InvalidOperationException($"Run {run.Id} of data refresh record {refreshRecord.Id} is gone.");
            var isAboveThreshold = summary.IsAboveThreshold(settings.MaxFailedDonorFraction);
            logger.Log(
                isAboveThreshold ? LogLevel.Error : LogLevel.Warning,
                LoggingPrefix + " Run {RunId} of data refresh record {DataRefreshRecordId} ({TargetDatabase}) is complete with failures: " +
                "{FailedDonorCount} of {TotalDonorCount} donors failed ({FailedDonorFraction:P3}). Above the threshold of " +
                "{MaxFailedDonorFraction:P3}: {IsAboveThreshold}. {PermanentlyFailedBatchCount} batch(es) failed permanently, and " +
                "{FailedGroupCount} group(s) failed in the other batches. The data refresh continues.",
                run.Id, refreshRecord.Id, database, summary.FailedDonorCount, summary.TotalDonorCount, summary.FailedDonorFraction,
                settings.MaxFailedDonorFraction, isAboveThreshold, summary.PermanentlyFailedBatchCount, summary.FailedGroupCount);

            await notificationSender.SendPrecomputationFailureSummary(refreshRecord.Id, run.Id, summary, settings.MaxFailedDonorFraction);
        }
        else
        {
            logger.LogInformation(
                LoggingPrefix + " Run {RunId} of data refresh record {DataRefreshRecordId} ({TargetDatabase}) is complete with no failure.",
                run.Id, refreshRecord.Id, database);
        }

        await repository.TruncateStagingTables();
    }

    private void LogProgress(DataRefreshRecord refreshRecord, TransientDatabase database, int runId, DonorGenotypePrecomputationBatchCounts counts) =>
        logger.LogInformation(
            LoggingPrefix + " Run {RunId} of data refresh record {DataRefreshRecordId} ({TargetDatabase}): {TerminalBatchCount} of " +
            "{BatchCount} batches done. {ResultsReceivedBatchCount} with results, {PermanentlyFailedBatchCount} permanently failed, " +
            "{InProgressBatchCount} in progress, {RequestedBatchCount} requested, {PendingBatchCount} pending, {FailedBatchCount} failed, " +
            "{AbandonedBatchCount} abandoned. {FailedGroupCount} group(s) failed.",
            runId, refreshRecord.Id, database, counts.TerminalBatchCount, counts.TotalBatchCount,
            counts.CountOf(BatchStatus.ResultsReceived), counts.CountOf(BatchStatus.PermanentlyFailed), counts.CountOf(BatchStatus.InProgress),
            counts.CountOf(BatchStatus.Requested), counts.CountOf(BatchStatus.Pending), counts.CountOf(BatchStatus.Failed),
            counts.CountOf(BatchStatus.Abandoned), counts.FailedGroupCount);

    private static string DescribeSteps(IEnumerable<DonorGenotypePrecomputationBuildStep> steps) =>
        string.Join("; ", steps.Select(step => $"{step.Name}: {step.RowCount} rows in {step.Duration.TotalSeconds:F1} s"));

    /// <summary>
    /// Follows the finished batches of a run, and tells when a stall starts and ends. A stall starts when no batch has
    /// finished for the stall alert time, so one stall gives one alert, however long it lasts.
    /// </summary>
    internal sealed class StallWatch(TimeSpan stallAlertTime, DateTimeOffset startTime)
    {
        internal enum Observation
        {
            /// <summary>No stall started or ended.</summary>
            None,

            /// <summary>No batch has finished for the stall alert time.</summary>
            StallStarted,

            /// <summary>A batch finished after a stall started.</summary>
            StallEnded
        }

        private int? lastTerminalBatchCount;
        private DateTimeOffset lastProgressTime = startTime;
        private bool isStalled;

        /// <param name="terminalBatchCount">The batches that are done, with results or not.</param>
        /// <param name="now">The time of the count.</param>
        public Observation Observe(int terminalBatchCount, DateTimeOffset now)
        {
            if (terminalBatchCount != lastTerminalBatchCount)
            {
                var wasStalled = isStalled;
                lastTerminalBatchCount = terminalBatchCount;
                lastProgressTime = now;
                isStalled = false;
                return wasStalled ? Observation.StallEnded : Observation.None;
            }

            if (isStalled || TimeWithoutProgress(now) < stallAlertTime)
            {
                return Observation.None;
            }

            isStalled = true;
            return Observation.StallStarted;
        }

        public TimeSpan TimeWithoutProgress(DateTimeOffset now) => now - lastProgressTime;
    }
}
