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
    }
}