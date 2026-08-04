namespace Atlas.MatchingAlgorithm.Settings
{
    public class DataRefreshSettings
    {
        /// <summary>
        /// When false, automatic running of the data refresh will be disabled
        /// </summary>
        public bool AutoRunDataRefresh { get; set; }
        
        public string ActiveDatabaseSize { get; set; }
        public string DormantDatabaseSize { get; set; }
        public string RefreshDatabaseSize { get; set; }
        
        public int DormantDatabaseAutoPauseTimeout { get; set; }
        public int ActiveDatabaseAutoPauseTimeout { get; set; }
        
        public string DatabaseAName { get; set; }
        public string DatabaseBName { get; set; }
        // ReSharper disable once UnusedMember.Global This property is only used in the Function TimerTrigger binding. Listed here for increased discoverability.
        public string CronTab { get; set; }

        /// <summary>
        /// Being fully Transactional is safer, but noticeably slower, due to limitations of how much we can parallelise.
        /// This is primarily limited by lack of Distributed Transaction support from .NET Core 3. See ATLAS-562.
        /// </summary>
        /// <seealso cref="DonorManagementSettings.OngoingDifferentialDonorUpdatesShouldBeFullyTransactional"/>
        public bool DataRefreshDonorUpdatesShouldBeFullyTransactional { get; set; }

        #region Batch size tunables

        // All four were hard-coded consts. They are surfaced as settings for two reasons, neither of which is
        // "change them": (a) so a run records its own batch geometry in the run manifest - a run that cannot describe
        // its own configuration cannot be compared with another run - and (b) so a follow-up A/B needs a setting
        // change rather than a deploy.
        //
        // They are NULLABLE on purpose. An absent config key binds to null rather than 0, and each consumer falls
        // back to the historic const. A non-nullable int would silently bind to 0 wherever the key is not configured -
        // and SqlBulkCopy.BatchSize = 0 means "one single batch of everything", which is a very different job.

        /// <summary>
        /// Donors read from the donor store per stage-40 batch. Defaults to <c>DonorImporter.DefaultBatchSize</c>.
        /// </summary>
        public int? DonorImportBatchSize { get; set; }

        /// <summary>
        /// Donors processed per stage-50 batch. Defaults to <c>HlaProcessor.DefaultBatchSize</c>.
        /// Note the undated folklore attached to this one: "At 4k it's been seen throwing OOM Exceptions".
        /// </summary>
        public int? HlaProcessingBatchSize { get; set; }

        /// <summary>
        /// Rows per <c>SqlBulkCopy</c> batch when writing donors / matching HLA.
        /// Defaults to <c>DataRefreshRepositorySettings.DefaultSqlBulkCopyBatchSize</c>.
        /// </summary>
        public int? SqlBulkCopyBatchSize { get; set; }

        /// <summary>
        /// Emit a human-readable stage-50 progress/ETA trace every N batches.
        /// Defaults to <c>HlaProcessor.DefaultBatchProgressReportingPeriod</c>.
        /// </summary>
        public int? BatchProgressReportingPeriod { get; set; }

        #endregion

        /// <summary>
        /// Name of topic where validated data refresh requests will be sent.
        /// </summary>
        public string RequestsTopic { get; set; }

        /// <summary>
        /// Name of topic where notifications of job completion (both success and failure) should be sent,
        /// in order to permit the automation of downstream tasks.
        /// Note: This is distinct from the support topics. Messages sent to <see cref="CompletionTopic"/> are designed
        /// to be consumed by automated workflows, whereas messages sent to the support topics are designed to be
        /// read by end-users in the support team.
        /// </summary>
        public string CompletionTopic { get; set; }
        public int SendRetryCount { get; set; }
        public int SendRetryCooldownSeconds { get; set; }

        /// <summary>
        /// How long the run-level lease on a data refresh record stays valid without renewal.
        /// Long enough that ordinary GC or CPU contention cannot displace a healthy owner, and short enough that an
        /// owner that has genuinely died does not block the next legitimate run for long.
        /// </summary>
        /// <remarks>
        /// Defaulted here rather than relying on configuration, so that an installation which has not yet had the
        /// corresponding app setting deployed still gets a usable lease rather than one that expires immediately.
        /// </remarks>
        public int LeaseDurationMinutes { get; set; } = 30;

        /// <summary>
        /// How often the invocation holding the lease renews it. Must leave room for several consecutive renewal
        /// failures within one <see cref="LeaseDurationMinutes"/>; see DataRefreshOrchestrator for the exact rule.
        /// </summary>
        /// <inheritdoc cref="LeaseDurationMinutes" path="/remarks"/>
        public int LeaseRenewalIntervalSeconds { get; set; } = 60;

        /// <summary>
        /// How long a refresh record must have shown no sign of life before the watchdog re-requests it. Must be longer
        /// than <see cref="LeaseDurationMinutes"/>; see DataRefreshWatchdog for the exact rule.
        /// </summary>
        /// <remarks>
        /// A margin on top of the lease rather than the thing that detects the stall, which an expired lease has
        /// already done. It covers what the lease cannot: the expiry is stamped by the clock of the host that ran the
        /// refresh and read here by another, so a shorter grace would let skew between the two decide whether a live
        /// run is re-requested. It also leaves a new record time for its request message to arrive.
        ///
        /// Defaulted here rather than relying on configuration, for the reason given on <see cref="LeaseDurationMinutes"/>.
        /// </remarks>
        public int WatchdogGraceDurationMinutes { get; set; } = 60;

        /// <summary>
        /// A crontab determining how often the watchdog sweeps for stalled refresh records. Recovering a leased record
        /// therefore takes up to <see cref="LeaseDurationMinutes"/> - the lease has to lapse before the grace period
        /// starts counting - plus <see cref="WatchdogGraceDurationMinutes"/>, plus one sweep interval.
        /// </summary>
        /// <remarks>
        /// Unlike the other lease and watchdog settings, this one cannot be defaulted here: the Functions host reads it
        /// to resolve the TimerTrigger binding. Without the app setting the host still starts and every other function
        /// in the app runs normally - only this one fails to index, and then never fires. A missing value therefore
        /// presents as a healthy app whose stalled refreshes are silently never recovered.
        /// </remarks>
        // ReSharper disable once UnusedMember.Global This property is only used in the Function TimerTrigger binding. Listed here for increased discoverability.
        public string WatchdogCronSchedule { get; set; }
    }
}