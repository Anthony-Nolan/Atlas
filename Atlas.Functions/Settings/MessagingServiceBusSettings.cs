namespace Atlas.Functions.Settings
{
    internal class MessagingServiceBusSettings
    {
        public string ConnectionString { get; set; }
        public string SearchResultsTopic { get; set; }
        public string RepeatSearchResultsTopic { get; set; }
        public int MatchingResultsSubscriptionMaxDeliveryCount { get; set; }
    }
}