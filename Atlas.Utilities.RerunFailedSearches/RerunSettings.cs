namespace Atlas.Utilities.RerunFailedSearches
{
    /// <summary>
    /// Connection strings and topic/subscription names, bound from the <c>Rerun</c> section of
    /// appsettings.json (or environment variables). All the topics involved live on the single Atlas
    /// Service Bus namespace, so one connection string covers both reading the results notifications and
    /// publishing the re-run requests.
    /// </summary>
    public class RerunSettings
    {
        /// <summary>Connection string for the Atlas Service Bus namespace (read + write).</summary>
        public string ServiceBusConnectionString { get; set; } = "";

        /// <summary>Connection string for the SearchTracking SQL database (the "PersistentSql" DB).</summary>
        public string SearchTrackingConnectionString { get; set; } = "";

        // ── Results notifications (read side) ─────────────────────────────────────────
        public string SearchResultsTopic { get; set; } = "search-results-ready";
        public string RepeatSearchResultsTopic { get; set; } = "repeat-search-results-ready";
        public string ResultsAuditSubscription { get; set; } = "audit";

        // ── Search requests (re-submit side) ──────────────────────────────────────────
        public string SearchRequestsTopic { get; set; } = "matching-requests";
        public string RepeatSearchRequestsTopic { get; set; } = "repeat-search-requests";
    }
}
