namespace Atlas.SearchTracking.Common.Settings.ServiceBus
{
    public class SearchTrackingServiceBusSettings
    {
        public string ConnectionString { get; set; }
        public string SearchTrackingTopic { get; set; }
        public int SendRetryCount { get; set; }
        public int SendRetryCooldownSeconds { get; set; }
    }
}
