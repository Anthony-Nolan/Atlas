namespace Atlas.MatchingAlgorithm.Settings
{
    public class DataRefreshSettings
    {
        public string ActiveDatabaseSize { get; set; }
        public string DormantDatabaseSize { get; set; }
        public string RefreshDatabaseSize { get; set; }
        public string DatabaseAName { get; set; }
        public string DatabaseBName { get; set; }
        // ReSharper disable once UnusedMember.Global This property is only used in the Function TimerTrigger binding. Listed here for increased discoverability.
        public string CronTab { get; set; }

        /// <summary>
        /// Being fully Transactional is safer, but noticeably slower, due to limitations of how much we can parallelise.
        /// This is primarily limited by lack of Distributed Transaction support from .NET Core 3. See ATLAS-QQQ.
        /// </summary>
        /// <seealso cref="DonorManagementSettings.OngoingDifferentialDonorUpdatesShouldBeFullyTransactional"/>
        public bool DataRefreshDonorUpdatesShouldBeFullyTransactional { get; set; }
    }
}