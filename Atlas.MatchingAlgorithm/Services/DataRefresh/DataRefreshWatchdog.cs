using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Client.Models.DataRefresh;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Exceptions;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Notifications;
using Atlas.MatchingAlgorithm.Settings;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh
{
    public interface IDataRefreshWatchdog
    {
        /// <summary>
        /// Re-publishes a refresh request for every record that has stalled, i.e. that is still open but that nothing
        /// is working on, and that therefore will never complete unaided.
        ///
        /// Without this, such a record is not merely stuck itself: <see cref="IDataRefreshRequester.RequestDataRefresh"/>
        /// refuses every new request while any open record exists, so a single stalled record halts all refreshes until
        /// someone intervenes by hand.
        /// </summary>
        /// <remarks>
        /// Deliberately only publishes. It takes no lease of its own - <see cref="IDataRefreshOrchestrator"/>'s claim on
        /// the record is what decides which invocation actually runs it, and re-requesting a record that has since been
        /// claimed, or completed, is refused there and costs nothing. Nor does it go through
        /// <see cref="IDataRefreshRequester"/>, which would create a second record rather than resume the stalled one.
        ///
        /// Per-record failures are logged and skipped so that one record cannot strand the rest of the sweep, then
        /// rethrown together, so that the invocation is still recorded as failed for alerting.
        /// </remarks>
        Task RecoverStalledRefreshes();
    }

    internal class DataRefreshWatchdog : IDataRefreshWatchdog
    {
        private const string LoggingPrefix = "DATA REFRESH WATCHDOG:";

        /// <summary>
        /// Deliberately distinct from anything the ordinary request path emits, so that a stall which healed itself can
        /// be counted and alerted on separately from a refresh that was requested normally. A stall is invisible
        /// otherwise: the record it leaves behind looks exactly like one that is legitimately still running.
        /// </summary>
        private const string StallRecoveredEventName = "Data refresh stall auto-recovered";

        private readonly IDataRefreshHistoryRepository dataRefreshHistoryRepository;
        private readonly IDataRefreshServiceBusClient serviceBusClient;
        private readonly DataRefreshSettings dataRefreshSettings;
        private readonly IMatchingAlgorithmImportLogger logger;

        public DataRefreshWatchdog(
            IDataRefreshHistoryRepository dataRefreshHistoryRepository,
            IDataRefreshServiceBusClient serviceBusClient,
            DataRefreshSettings dataRefreshSettings,
            IMatchingAlgorithmImportLogger logger)
        {
            this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
            this.serviceBusClient = serviceBusClient;
            this.dataRefreshSettings = dataRefreshSettings;
            this.logger = logger;
        }

        public async Task RecoverStalledRefreshes()
        {
            var graceCutoffUtc = DateTime.UtcNow - ValidatedGraceDuration();

            var stalledRecordIds = await dataRefreshHistoryRepository.GetStalledRefreshRecordIds(graceCutoffUtc);
            if (stalledRecordIds.Count == 0)
            {
                return;
            }

            logger.SendTrace(
                $"{LoggingPrefix} Found {stalledRecordIds.Count} stalled data refresh record(s), idle since before {graceCutoffUtc:u}. " +
                "Re-requesting each; whichever invocation claims a record will resume it from its last completed stage.", LogLevel.Warn);

            var failures = new List<Exception>();
            foreach (var recordId in stalledRecordIds)
            {
                try
                {
                    await ReRequestStalledRecord(recordId, graceCutoffUtc);
                }
                catch (Exception e)
                {
                    // Nothing has been written to the record, so the next sweep will simply find it again and retry.
                    logger.SendTrace(
                        $"{LoggingPrefix} Failed to re-request stalled data refresh record {recordId}. Exception: {e}", LogLevel.Error);
                    failures.Add(new InvalidOperationException($"Failed to re-request stalled data refresh record {recordId}.", e));
                }
            }

            if (failures.Count > 0)
            {
                throw new AggregateException(
                    $"{failures.Count} of {stalledRecordIds.Count} stalled data refresh record(s) could not be re-requested in this sweep.",
                    failures);
            }
        }

        private async Task ReRequestStalledRecord(int recordId, DateTime graceCutoffUtc)
        {
            await serviceBusClient.PublishToRequestTopic(new ValidatedDataRefreshRequest { DataRefreshRecordId = recordId });

            // Sent only once the publish has succeeded, so the event means "a request is on the topic", not "one was attempted".
            logger.SendEvent(StallRecoveredEventName, LogLevel.Warn, new Dictionary<string, string>
            {
                { "DataRefreshRecordId", recordId.ToString() },
                { "GraceCutoffUtc", graceCutoffUtc.ToString("u") }
            });
        }

        /// <summary>
        /// The grace period must outlast the lease, so that an expired lease is never the only thing separating a dead
        /// owner from a live one. <see cref="DataRefreshSettings.WatchdogGraceDurationMinutes"/> records why.
        /// </summary>
        private TimeSpan ValidatedGraceDuration()
        {
            var graceDuration = TimeSpan.FromMinutes(dataRefreshSettings.WatchdogGraceDurationMinutes);
            var leaseDuration = TimeSpan.FromMinutes(dataRefreshSettings.LeaseDurationMinutes);

            if (graceDuration <= TimeSpan.Zero || graceDuration <= leaseDuration)
            {
                throw new InvalidDataRefreshConfigurationException(
                    $"Data refresh watchdog settings are invalid. {nameof(DataRefreshSettings.WatchdogGraceDurationMinutes)} " +
                    $"({dataRefreshSettings.WatchdogGraceDurationMinutes}) must be positive, and must exceed " +
                    $"{nameof(DataRefreshSettings.LeaseDurationMinutes)} ({dataRefreshSettings.LeaseDurationMinutes}), or a refresh that " +
                    "is merely between lease renewals could be mistaken for one that has stalled.");
            }

            return graceDuration;
        }
    }
}
