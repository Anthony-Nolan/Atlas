namespace Atlas.RepeatSearch.Settings.ServiceBus
{
    public class MessagingServiceBusSettings
    {
        public string ConnectionString { get; set; }
        public string RepeatSearchRequestsTopic { get; set; }
        public string RepeatSearchRequestsSubscription { get; set; }
        public string RepeatSearchMatchingResultsTopic { get; set; }
        public int RepeatSearchRequestsMaxDeliveryCount { get; set; }

        /// <summary>
        /// Subscription for matching notifications used by debug endpoint.
        /// </summary>
        public string RepeatSearchResultsDebugSubscription { get; set; }
    }
}
