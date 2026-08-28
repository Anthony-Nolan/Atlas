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
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Notifications;
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
        private readonly IServiceScopeFactory serviceScopeFactory;

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
            IServiceScopeFactory serviceScopeFactory)
        {
            this.logger = logger;
            this.dataRefreshSettings = dataRefreshSettings;
            this.activeDatabaseProvider = activeDatabaseProvider;
            this.dataRefreshRunner = dataRefreshRunner;
            this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
            this.azureDatabaseManager = azureDatabaseManager;
            this.azureDatabaseNameProvider = azureDatabaseNameProvider;
            this.dataRefreshNotificationSender = dataRefreshNotificationSender;
            this.dataRefreshCompletionNotifier = dataRefreshCompletionNotifier;
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

            try
            {
                var incompleteJob = FetchIncompleteJobRecord(dataRefreshRecordId);

                await dataRefreshNotificationSender.SendInProgressNotification(
                    dataRefreshRecordId, 1 + incompleteJob.RefreshAttemptedCount);

                await ContinueRefreshJob(dataRefreshRecordId, cancellationTokenSource.Token);
            }
            finally
            {
                // Order matters: the heartbeat must be stopped and awaited before the lease is released. Releasing while
                // a renewal is still in flight would let that renewal land afterwards and resurrect a lease that nothing
                // is holding, blocking the next run until it expired.
                await cancellationTokenSource.CancelAsync();
                await ObserveHeartbeat(heartbeat, dataRefreshRecordId);
                await ReleaseLease(dataRefreshRecordId, leaseOwner);
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
                return record.RefreshEndUtc != null
                    ? $"The record was already completed at {record.RefreshEndUtc:u}."
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
        private async Task ContinueRefreshJob(int dataRefreshRecordId, CancellationToken cancellationToken)
        {
            try
            {
                await dataRefreshHistoryRepository.UpdateRunAttemptDetails(dataRefreshRecordId);
                var newWmdaHlaNomenclatureVersion = await dataRefreshRunner.RefreshData(dataRefreshRecordId, cancellationToken);
                var previouslyActiveDatabase = azureDatabaseNameProvider.GetDatabaseName(activeDatabaseProvider.GetActiveDatabase());
                await MarkDataHistoryRecordAsComplete(dataRefreshRecordId, true, newWmdaHlaNomenclatureVersion);
                await ScaleDownDatabaseToDormantLevel(previouslyActiveDatabase);
                await dataRefreshCompletionNotifier.NotifyOfSuccess(dataRefreshRecordId);
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
                await dataRefreshCompletionNotifier.NotifyOfFailure(dataRefreshRecordId);
                await MarkDataHistoryRecordAsComplete(dataRefreshRecordId, false, null);
            }
        }

        private async Task ScaleDownDatabaseToDormantLevel(string databaseName)
        {
            var dormantSize = dataRefreshSettings.DormantDatabaseSize.ParseToEnum<AzureDatabaseSize>();
            var dormantAutoPause = dataRefreshSettings.DormantDatabaseAutoPauseTimeout;
            logger.SendTrace($"DATA REFRESH TEAR DOWN: Scaling down database: {databaseName} to dormant size: {dormantSize}");
            await azureDatabaseManager.UpdateDatabaseSize(databaseName, dormantSize, dormantAutoPause);
        }

        private async Task MarkDataHistoryRecordAsComplete(int recordId, bool wasSuccess, string wmdaHlaNomenclatureVersion)
        {
            await dataRefreshHistoryRepository.UpdateExecutionDetails(recordId, wmdaHlaNomenclatureVersion, DateTime.UtcNow);
            await dataRefreshHistoryRepository.UpdateSuccessFlag(recordId, wasSuccess);
        }
    }
}