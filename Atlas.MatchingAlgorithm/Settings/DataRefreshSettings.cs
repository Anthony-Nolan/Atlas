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
    }
}