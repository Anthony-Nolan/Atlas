namespace Atlas.Common.Notifications
{
    public class NotificationsServiceBusSettings
    {
        public string ConnectionString { get; set; }
        public string AlertsTopic { get; set; }
        public string NotificationsTopic { get; set; }
        public int SendRetryCount { get; set; }
        public int SendRetryCooldownSeconds { get; set; }
    }
}