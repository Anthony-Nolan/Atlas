namespace Atlas.Functions.Settings
{
    internal class MessagingServiceBusSettings
    {
        public string ConnectionString { get; set; }
        public string SearchResultsTopic { get; set; }
        public string RepeatSearchResultsTopic { get; set; }

        /// <summary>
        /// Required by debug endpoint that peeks `search-results-ready` messages.
        /// </summary>
        public string SearchResultsDebugSubscription { get; set; }
    }
}