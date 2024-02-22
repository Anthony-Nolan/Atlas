namespace Atlas.MatchingAlgorithm.Settings.ServiceBus
{
    public class MessagingServiceBusSettings
    {
        public string ConnectionString { get; set; }

        // TODO: ATLAS-914: - Messaging service bus conn string also used by data management & refresh, so these search-only props should be split to their own settings model.
        public int SearchRequestsMaxDeliveryCount { get; set; }
        public string SearchRequestsTopic { get; set; }
        public string SearchRequestsSubscription { get; set; }
        public string SearchResultsTopic { get; set; }

        /// <summary>
        /// Required by debug endpoint that peeks result notifications.
        /// </summary>
        public string SearchResultsDebugSubscription { get; set; }
    }
}