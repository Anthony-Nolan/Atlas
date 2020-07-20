namespace Atlas.MatchingAlgorithm.Settings
{
    public class DonorManagementSettings
    {
        public int BatchSize { get; set; }
        public string Topic { get; set; }
        public string SubscriptionForDbA { get; set; }
        public string SubscriptionForDbB { get; set; }
        // ReSharper disable once UnusedMember.Global This property is only used in the Function TimerTrigger binding. Listed here for increased discoverability.
        public string CronSchedule { get; set; }

        /// <summary>
        /// Being fully Transactional is safer, but noticeably slower, due to limitations of how much we can parallelise.
        /// This is primarily limited by lack of Distributed Transaction support from .NET Core 3. See ATLAS-QQQ.
        /// </summary>
        /// <seealso cref="DataRefreshSettings.DataRefreshDonorUpdatesShouldBeFullyTransactional"/>
        public bool OngoingDifferentialDonorUpdatesShouldBeFullyTransactional { get; set; }
    }
}
