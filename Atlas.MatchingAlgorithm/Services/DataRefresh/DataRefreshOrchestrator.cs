using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Exceptions;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Atlas.MatchingAlgorithm.Models.DataRefresh;
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Notifications;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchingAlgorithm.Settings;
using EnumStringValues;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh
{
    public interface IDataRefreshOrchestrator
    {
        /// <summary>
        /// If there is exactly one incomplete data refresh record, the method will pick up from the last successful stage.
        ///
        /// Only one invocation may process a given record at a time. This is enforced by a run-level lease held on the
        /// record itself: an invocation that cannot claim the lease logs the fact and returns without running any stage.
        ///
        /// The lease is what makes that safe to rely on. A refresh runs for far longer than an Azure Service Bus message
        /// lock can be held, so the request message can be redelivered - and a second invocation started - while the
        /// first is still running. Nothing in the message or in the record's own data distinguishes
        /// "a single job is unfinished, and is actively running" from
        /// "a single job is unfinished, but is not actively running due to an interruption"; an unexpired lease does.
        /// </summary>
        /// <param name="invocationId">
        /// Identifies the calling invocation, and becomes the lease owner recorded against the refresh record. Supplied
        /// by the caller so that the entry telemetry it logs and the LeaseOwner column hold the same value, which is what
        /// lets a query over the two establish whether invocations actually overlapped. Generated here if not supplied.
        /// </param>
        Task OrchestrateDataRefresh(int dataRefreshRecordId, Guid? invocationId = null);
    }

    internal class DataRefreshOrchestrator : IDataRefreshOrchestrator
    {
        private const string LoggingPrefix = "DATA REFRESH:";

        private readonly IMatchingAlgorithmImportLogger logger;
        private readonly DataRefreshSettings dataRefreshSettings;
        private readonly IDataRefreshRunner dataRefreshRunner;
        private readonly IDataRefreshHistoryRepository dataRefreshHistoryRepository;
        private readonly IAzureDatabaseManager azureDatabaseManager;
        private readonly IActiveDatabaseProvider activeDatabaseProvider;
        private readonly IAzureDatabaseNameProvider azureDatabaseNameProvider;
        private readonly IDataRefreshSupportNotificationSender dataRefreshNotificationSender;
        private readonly IDataRefreshCompletionNotifier dataRefreshCompletionNotifier;
        private readonly IDonorGenotypePrecomputationRunCanceller precomputationRunCanceller;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly IDataRefreshManualDecisionEvents manualDecisionEvents;

        public DataRefreshOrchestrator(
            IMatchingAlgorithmImportLogger logger,
            DataRefreshSettings dataRefreshSettings,
            IActiveDatabaseProvider activeDatabaseProvider,
            IDataRefreshRunner dataRefreshRunner,
            IDataRefreshHistoryRepository dataRefreshHistoryRepository,
            IAzureDatabaseManager azureDatabaseManager,
            IAzureDatabaseNameProvider azureDatabaseNameProvider,
            IDataRefreshSupportNotificationSender dataRefreshNotificationSender,
            IDataRefreshCompletionNotifier dataRefreshCompletionNotifier,
            IDonorGenotypePrecomputationRunCanceller precomputationRunCanceller,
            IServiceScopeFactory serviceScopeFactory,
            IDataRefreshManualDecisionEvents manualDecisionEvents)
        {
            this.manualDecisionEvents = manualDecisionEvents;
            this.logger = logger;
            this.dataRefreshSettings = dataRefreshSettings;
            this.activeDatabaseProvider = activeDatabaseProvider;
            this.dataRefreshRunner = dataRefreshRunner;
            this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
            this.azureDatabaseManager = azureDatabaseManager;
            this.azureDatabaseNameProvider = azureDatabaseNameProvider;
            this.dataRefreshNotificationSender = dataRefreshNotificationSender;
            this.dataRefreshCompletionNotifier = dataRefreshCompletionNotifier;
            this.precomputationRunCanceller = precomputationRunCanceller;
            this.serviceScopeFactory = serviceScopeFactory;
        }

        public async Task OrchestrateDataRefresh(int dataRefreshRecordId, Guid? invocationId)
        {
            var (leaseDuration, renewalInterval) = ValidatedLeaseTimings();
            var leaseOwner = invocationId ?? Guid.NewGuid();

            if (!await dataRefreshHistoryRepository.TryClaimRefreshLease(dataRefreshRecordId, leaseOwner, DateTime.UtcNow, leaseDuration))
            {
                await LogRefusedClaim(dataRefreshRecordId, leaseOwner);
                return;
            }

            using var cancellationTokenSource = new CancellationTokenSource();
            var heartbeat = MaintainLease(dataRefreshRecordId, leaseOwner, leaseDuration, renewalInterval, cancellationTokenSource);

            DonorGenotypePrecomputationFailureReport pauseReport = null;
            try
            {
                var incompleteJob = FetchIncompleteJobRecord(dataRefreshRecordId);

                await dataRefreshNotificationSender.SendInProgressNotification(
                    dataRefreshRecordId, 1 + incompleteJob.RefreshAttemptedCount);

                pauseReport = await ContinueRefreshJob(incompleteJob, cancellationTokenSource.Token);
            }
            finally
            {
                // Order matters: the heartbeat must be stopped and awaited before the lease is released. Releasing while
                // a renewal is still in flight would let that renewal land afterwards and resurrect a lease that nothing
                // is holding, blocking the next run until it expired.
                await cancellationTokenSource.CancelAsync();
                await ObserveHeartbeat(heartbeat, dataRefreshRecordId);

                if (pauseReport != null)
                {
                    await PauseAndReleaseLease(dataRefreshRecordId, leaseOwner, pauseReport);
                }
                else
                {
                    await ReleaseLease(dataRefreshRecordId, leaseOwner);
                }
            }
        }

        /// <summary>
        /// The renewal interval must leave room for several consecutive renewal failures within a single lease, or the
        /// first transient blip would hand the record to another invocation. Requiring at least two renewal attempts per
        /// lease is the minimum that makes the lease meaningfully more robust than the message lock it replaces; the
        /// default settings allow thirty.
        /// </summary>
        private (TimeSpan LeaseDuration, TimeSpan RenewalInterval) ValidatedLeaseTimings()
        {
            var leaseDuration = TimeSpan.FromMinutes(dataRefreshSettings.LeaseDurationMinutes);
            var renewalInterval = TimeSpan.FromSeconds(dataRefreshSettings.LeaseRenewalIntervalSeconds);

            if (renewalInterval <= TimeSpan.Zero || leaseDuration <= TimeSpan.Zero || renewalInterval + renewalInterval > leaseDuration)
            {
                throw new InvalidDataRefreshConfigurationException(
                    $"Data refresh lease settings are invalid. {nameof(DataRefreshSettings.LeaseDurationMinutes)} " +
                    $"({dataRefreshSettings.LeaseDurationMinutes}) and {nameof(DataRefreshSettings.LeaseRenewalIntervalSeconds)} " +
                    $"({dataRefreshSettings.LeaseRenewalIntervalSeconds}) must both be positive, and the lease must last at least " +
                    "two renewal intervals.");
            }

            return (leaseDuration, renewalInterval);
        }

        private async Task LogRefusedClaim(int dataRefreshRecordId, Guid leaseOwner)
        {
            // Deliberately does not throw: throwing here would propagate out of the function and cause the very Service
            // Bus redelivery that this lease exists to make harmless.
            var currentOwner = await TryDescribeCurrentOwner(dataRefreshRecordId);
            logger.SendTrace(
                $"{LoggingPrefix} Invocation {leaseOwner} could not claim data refresh record {dataRefreshRecordId}, so will not run any stage. {currentOwner}");
        }

        private async Task<string> TryDescribeCurrentOwner(int dataRefreshRecordId)
        {
            try
            {
                var record = await dataRefreshHistoryRepository.GetRecord(dataRefreshRecordId);
                if (record.RefreshEndUtc != null)
                {
                    return $"The record was already completed at {record.RefreshEndUtc:u}.";
                }

                return record.AwaitingManualDecisionSinceUtc != null
                    ? $"It is paused for a manual decision since {record.AwaitingManualDecisionSinceUtc:u}. Send the decision to the " +
                      $"{ResolvePausedDataRefreshRequest.EndpointName} endpoint."
                    : $"It is held by invocation {record.LeaseOwner}, until {record.LeaseExpiresUtc:u}.";
            }
            catch (Exception e)
            {
                return $"The current lease holder could not be read. Exception: {e}";
            }
        }

        /// <summary>
        /// Keeps this invocation's lease alive for as long as the refresh is running, and aborts the run if the lease is
        /// lost.
        /// </summary>
        /// <remarks>
        /// Renews from a dedicated task rather than inline in the refresh stages. Individual stage batches take several
        /// minutes each, and some stages do heavy synchronous work on the calling thread, so an inline heartbeat would
        /// fail to renew under exactly the conditions where renewal matters most.
        ///
        /// Each tick resolves its own DI scope, because the scoped <see cref="IDataRefreshHistoryRepository"/> wraps a
        /// DbContext that is shared by the whole invocation - <see cref="DataRefreshRunner"/> holds the refresh record
        /// change-tracked for the entire run - and a DbContext cannot be used from two threads at once.
        /// </remarks>
        private async Task MaintainLease(
            int recordId,
            Guid leaseOwner,
            TimeSpan leaseDuration,
            TimeSpan renewalInterval,
            CancellationTokenSource cancellationTokenSource)
        {
            var lastSuccessfulRenewal = DateTime.UtcNow;

            while (true)
            {
                try
                {
                    await Task.Delay(renewalInterval, cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    // The run has finished, and the caller is about to release the lease.
                    return;
                }

                try
                {
                    var now = DateTime.UtcNow;
                    if (await RenewLease(recordId, leaseOwner, now + leaseDuration))
                    {
                        lastSuccessfulRenewal = now;
                        continue;
                    }

                    logger.SendTrace(
                        $"{LoggingPrefix} Invocation {leaseOwner} no longer holds the lease on record {recordId}; it has been taken over by " +
                        "another invocation. Aborting this run.", LogLevel.Critical);
                    await cancellationTokenSource.CancelAsync();
                    return;
                }
                catch (Exception e)
                {
                    // Being unable to reach the database is not the same as being fenced, and must not be treated as
                    // such. Transient failures have already been retried with backoff inside the call, by EF's execution
                    // strategy (see EnableRetryOnFailure in ContextFactory), so what reaches here is either non-transient
                    // or has exhausted those retries. Deliberately no further backoff on top: the fixed tick is what
                    // gives 30 renewal attempts per lease, so 29 consecutive failures are survivable - the whole
                    // improvement over the Service Bus lock this replaces, where a single missed renewal was permanent.
                    // Backing off would spend the same lease on a handful of attempts instead. Past the lease duration,
                    // though, the lease has expired and another invocation may already have claimed the record, so we
                    // must stop rather than run on as a zombie.
                    var timeSinceLastRenewal = DateTime.UtcNow - lastSuccessfulRenewal;
                    if (timeSinceLastRenewal < leaseDuration)
                    {
                        logger.SendTrace(
                            $"{LoggingPrefix} Failed to renew the lease on record {recordId}, last renewed " +
                            $"{timeSinceLastRenewal.TotalSeconds:F0}s ago. Will retry. Exception: {e}", LogLevel.Error);
                        continue;
                    }

                    logger.SendTrace(
                        $"{LoggingPrefix} Have been unable to renew the lease on record {recordId} for " +
                        $"{timeSinceLastRenewal.TotalMinutes:F0} minutes, exceeding the lease duration. The lease must be assumed lost. " +
                        $"Aborting this run. Exception: {e}", LogLevel.Critical);
                    await cancellationTokenSource.CancelAsync();
                    return;
                }
            }
        }

        private async Task<bool> RenewLease(int recordId, Guid leaseOwner, DateTime expiry)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var historyRepository = scope.ServiceProvider.GetRequiredService<IDataRefreshHistoryRepository>();
            return await historyRepository.TryRenewRefreshLease(recordId, leaseOwner, expiry);
        }

        private async Task ObserveHeartbeat(Task heartbeat, int recordId)
        {
            try
            {
                await heartbeat;
            }
            catch (Exception e)
            {
                logger.SendTrace($"{LoggingPrefix} Lease heartbeat for record {recordId} terminated unexpectedly. Exception: {e}", LogLevel.Error);
            }
        }

        private async Task ReleaseLease(int recordId, Guid leaseOwner)
        {
            try
            {
                if (!await dataRefreshHistoryRepository.ReleaseRefreshLease(recordId, leaseOwner))
                {
                    logger.SendTrace(
                        $"{LoggingPrefix} Invocation {leaseOwner} released no lease on record {recordId}, having already been fenced by another " +
                        "invocation.");
                }
            }
            catch (Exception e)
            {
                // Not fatal. The lease expires on its own, so a failed release delays the next legitimate run by at most
                // the lease duration rather than blocking it.
                logger.SendTrace(
                    $"{LoggingPrefix} Failed to release the lease on record {recordId}. It will expire on its own. Exception: {e}",
                    LogLevel.Error);
            }
        }

        /// <summary>
        /// Pauses the record for a manual decision, and releases the lease, in one update fenced by the lease owner. Does
        /// not throw.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The runner has scaled the database down and sent the paused alert already. So a paused record always has its
        /// alert, and never has a lease: a decision can start the resumed refresh at once.
        /// </para>
        ///
        /// <para>
        /// When the update fails, the lease is released as usual. The record is then open and not paused, so the watchdog
        /// resumes it, and the pause starts again.
        /// </para>
        /// </remarks>
        private async Task PauseAndReleaseLease(int recordId, Guid leaseOwner, DonorGenotypePrecomputationFailureReport pauseReport)
        {
            bool wasPaused;
            try
            {
                wasPaused = await dataRefreshHistoryRepository.TryPauseAndReleaseLease(recordId, leaseOwner, DateTime.UtcNow);
            }
            catch (Exception e)
            {
                logger.SendTrace(
                    $"{LoggingPrefix} Failed to pause record {recordId} for a manual decision. The lease is released instead, and the " +
                    $"watchdog resumes the record later: the pause then starts again. Exception: {e}", LogLevel.Error);
                await ReleaseLease(recordId, leaseOwner);
                return;
            }

            if (!wasPaused)
            {
                logger.SendTrace(
                    $"{LoggingPrefix} Invocation {leaseOwner} did not pause record {recordId}: it no longer holds the lease, or the record " +
                    "is closed.", LogLevel.Warn);
                return;
            }

            manualDecisionEvents.Paused(recordId, pauseReport);
            logger.SendTrace(
                $"{LoggingPrefix} Record {recordId} is paused, and waits for a manual decision on the " +
                $"{ResolvePausedDataRefreshRequest.EndpointName} endpoint. No invocation can claim it until then.", LogLevel.Warn);
        }

        /// <remarks>
        /// With the lease claimed ahead of this check, a request naming a record that is already finished, or that names
        /// a different record from the one in progress, has already been turned away. What remains is the genuine data
        /// error of more than one open job record, which should still fail loudly.
        /// </remarks>
        private DataRefreshRecord FetchIncompleteJobRecord(int dataRefreshRecordId)
        {
            var errorMessagePrefix = $"Cannot run data refresh {dataRefreshRecordId}. ";

            var incompleteJobs = dataRefreshHistoryRepository.GetIncompleteRefreshJobs().ToList();
            switch (incompleteJobs.Count)
            {
                case 0:
                    throw new InvalidDataRefreshRequestHttpException($"{errorMessagePrefix}There is no record of an initiated job. " +
                                                                 "Please submit a new data refresh request.");
                case 1:
                    var incompleteJob = incompleteJobs.Single();
                    if (incompleteJob.Id != dataRefreshRecordId)
                    {
                        throw new InvalidDataRefreshRequestHttpException($"{errorMessagePrefix}In-progress job has ID of {incompleteJob.Id}.");
                    }

                    //TODO: ATLAS-335: Check continuation 'signature' input.
                    return incompleteJob;

                default:
                    throw new InvalidDataRefreshRequestHttpException($"{errorMessagePrefix}More than one open job record found. " +
                                                                 "Please manually clean up refresh records.");
            }
        }

        /// <summary>
        /// Refresh job will be "continued" from the appropriate point, including on the first attempt.
        /// </summary>
        /// <returns>
        /// The failures that the refresh paused for, or null when it did not pause. The caller sets the pause, in the update
        /// that releases the lease.
        /// </returns>
        private async Task<DonorGenotypePrecomputationFailureReport> ContinueRefreshJob(DataRefreshRecord incompleteJob, CancellationToken cancellationToken)
        {
            var dataRefreshRecordId = incompleteJob.Id;

            try
            {
                await dataRefreshHistoryRepository.UpdateRunAttemptDetails(dataRefreshRecordId);
                var result = await dataRefreshRunner.RefreshData(dataRefreshRecordId, cancellationToken);
                if (result.IsPaused)
                {
                    // The record stays open, with its run and its staging data: the manual decision decides how it ends. So no
                    // close, no completion notification, and no cancel of the run.
                    logger.SendTrace(
                        $"{LoggingPrefix} Record {dataRefreshRecordId} pauses for a manual decision. It is not closed.", LogLevel.Warn);
                    return result.PauseReport;
                }

                var previouslyActiveDatabase = azureDatabaseNameProvider.GetDatabaseName(activeDatabaseProvider.GetActiveDatabase());
                await MarkDataHistoryRecordAsComplete(dataRefreshRecordId, true, result.HlaNomenclatureVersion);
                await ScaleDownDatabaseToDormantLevel(dataRefreshRecordId, previouslyActiveDatabase);
                await NotifyOfCompletion(dataRefreshRecordId, true);

                // After a manual Complete decision, the staging data of the precomputation stage is still in the database that is
                // now active. Normally the tables are empty already, and this costs nothing. Last, because it must not delay the
                // notifications.
                await precomputationRunCanceller.RemoveStagingData(incompleteJob);
                logger.SendTrace("Data Refresh Succeeded.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // The lease was lost, so this record now belongs to whichever invocation took it over. Deliberately
                // neither marks the record complete nor notifies of failure: either would sabotage that invocation, by
                // closing the record out from under it and reporting a failure that has not happened.
                logger.SendTrace(
                    $"{LoggingPrefix} Data Refresh aborted: record {dataRefreshRecordId} is no longer owned by this invocation.",
                    LogLevel.Critical);
            }
            catch (SqlException e)
            {
                logger.SendTrace($"Data Refresh Error: ${e}", LogLevel.Error);
                throw; // we are re-throwing the exception to allow automatic retry of the job
            }
            catch (Exception e)
            {
                logger.SendTrace($"Data Refresh Failed: ${e}", LogLevel.Critical);

                // Close the record before the notifications. If a notification failed first, the record would stay open, and the
                // next delivery of the request would claim it and run the failed refresh again.
                await MarkDataHistoryRecordAsComplete(dataRefreshRecordId, false, null);
                await NotifyOfCompletion(dataRefreshRecordId, false);

                // Only here: after a SqlException the request is delivered again and the stage continues its run, and after a lost
                // lease the record belongs to another invocation. Last, because the removal of the staging data waits for the
                // statements of the workers, and must not delay the failure notifications.
                await precomputationRunCanceller.CancelRun(incompleteJob);
            }

            return null;
        }

        /// <summary>
        /// Scales the database that was active before this refresh down to the dormant size. Does not throw.
        /// </summary>
        /// <remarks>
        /// This runs after the record is closed as successful, when the refreshed database is already the active one. An exception
        /// that got to the generic catch in <see cref="ContinueRefreshJob"/> would mark the record as failed, and searches would go
        /// back to this database. So a failure is logged and alerted instead, as the failure teardown in <see cref="DataRefreshRunner"/>
        /// does.
        /// </remarks>
        private async Task ScaleDownDatabaseToDormantLevel(int dataRefreshRecordId, string databaseName)
        {
            try
            {
                var dormantSize = dataRefreshSettings.DormantDatabaseSize.ParseToEnum<AzureDatabaseSize>();
                var dormantAutoPause = dataRefreshSettings.DormantDatabaseAutoPauseTimeout;
                logger.SendTrace($"DATA REFRESH TEAR DOWN: Scaling down database: {databaseName} to dormant size: {dormantSize}");
                await azureDatabaseManager.UpdateDatabaseSize(databaseName, dormantSize, dormantAutoPause);
            }
            catch (Exception e)
            {
                logger.SendTrace(
                    $"DATA REFRESH TEAR DOWN: Failed to scale down database {databaseName}. The refresh succeeded, but this database must " +
                    $"be scaled down manually. Exception: {e}", LogLevel.Critical);
                await dataRefreshNotificationSender.SendTeardownFailureAlert(dataRefreshRecordId);
            }
        }

        /// <summary>
        /// Sends the support notification and the completion message for a record that is already closed. Does not throw.
        /// </summary>
        /// <remarks>
        /// The record already holds the result, and a failure to notify must not change it. An exception that got to the generic
        /// catch in <see cref="ContinueRefreshJob"/> would report a successful refresh as failed. An exception that left the
        /// invocation would fail the function run, and Service Bus would deliver the request again for no reason.
        /// </remarks>
        private async Task NotifyOfCompletion(int dataRefreshRecordId, bool wasSuccessful)
        {
            try
            {
                if (wasSuccessful)
                {
                    await dataRefreshCompletionNotifier.NotifyOfSuccess(dataRefreshRecordId);
                }
                else
                {
                    await dataRefreshCompletionNotifier.NotifyOfFailure(dataRefreshRecordId);
                }
            }
            catch (Exception e)
            {
                var result = wasSuccessful ? "successful" : "failed";
                logger.SendTrace(
                    $"{LoggingPrefix} Record {dataRefreshRecordId} is closed as {result}, but its completion message could not be published. " +
                    $"Consumers of the completion topic will get no message for this job. Exception: {e}", LogLevel.Error);
            }
        }

        private async Task MarkDataHistoryRecordAsComplete(int recordId, bool wasSuccess, string wmdaHlaNomenclatureVersion)
        {
            await dataRefreshHistoryRepository.UpdateExecutionDetails(recordId, wmdaHlaNomenclatureVersion, DateTime.UtcNow);
            await dataRefreshHistoryRepository.UpdateSuccessFlag(recordId, wasSuccess);
        }
    }
}