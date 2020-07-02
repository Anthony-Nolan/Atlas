namespace Atlas.MatchingAlgorithm.Settings
{
    public class DonorManagementSettings
    {
        public string BatchSize { get; set; }
        public string Topic { get; set; }
        public string SubscriptionForDbA { get; set; }
        public string SubscriptionForDbB { get; set; }
        // ReSharper disable once UnusedMember.Global This property is only used in the Function TimerTrigger binding. Listed here for increased discoverability.
        public string CronSchedule { get; set; }

    }
}
