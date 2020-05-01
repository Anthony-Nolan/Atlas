namespace Atlas.MatchingAlgorithm.ConfigSettings
{
    public class NotificationsServiceBusSettings
    {
        public string ConnectionString { get; set; }
        public string AlertsTopic { get; set; }
        public string NotificationsTopic { get; set; }
    }
}