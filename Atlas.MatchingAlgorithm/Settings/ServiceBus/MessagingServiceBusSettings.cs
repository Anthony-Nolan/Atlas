namespace Atlas.MatchingAlgorithm.Settings.ServiceBus
{
    public class MessagingServiceBusSettings
    {
        public string ConnectionString { get; set; }

        // TODO ATLAS-472 - Messaging service bus conn string also used by data management & refresh,
        // so these search-only props should be split to their own settings model.
        public string SearchRequestsQueue { get; set; }
        public string SearchResultsTopic { get; set; }
    }
}