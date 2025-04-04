namespace Atlas.Functions.Settings
{
    internal class MessagingServiceBusSettings
    {
        public string ConnectionString { get; set; }
        public string SearchResultsTopic { get; set; }
        public string RepeatSearchResultsTopic { get; set; }
        public int SendRetryCount { get; set; }
        public int SendRetryCooldownSeconds { get; set; }

        /// <summary>
        /// Required by debug endpoint that peeks `search-results-ready` messages.
        /// </summary>
        public string SearchResultsDebugSubscription { get; set; }

        /// <summary>
        /// Required by debug endpoint that peeks `repeat-search-results-ready` messages.
        /// </summary>
        public string RepeatSearchResultsDebugSubscription { get; set; }
    }
}