namespace Atlas.MatchingAlgorithm.Settings.ServiceBus
{
    public class MessagingServiceBusSettings
    {
        public string ConnectionString { get; set; }
        public string SearchRequestsQueue { get; set; }
        public string SearchResultsTopic { get; set; }
    }
}