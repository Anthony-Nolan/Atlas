namespace Atlas.MatchPrediction.ExternalInterface.Settings
{
    public class MessagingServiceBusSettings
    {
        public string ConnectionString { get; set; }

        public int SendRetryCount { get; set; }

        public int SendRetryCooldownSeconds { get; set; }
    }
}