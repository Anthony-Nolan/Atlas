namespace Atlas.Functions.Settings;

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

    /// <summary>Topic to which the orchestrator publishes batch-blob locations for the parallel ACA Worker MPA path.</summary>
    public string ParallelMatchPredictionRequestsTopic { get; set; }

    /// <summary>Topic on which the ACA Worker publishes batch results for the parallel MPA path.</summary>
    public string ParallelMatchPredictionResultsTopic { get; set; }

    /// <summary>Subscription name on <see cref="ParallelMatchPredictionResultsTopic"/> used by the aggregator function.</summary>
    public string ParallelMatchPredictionResultsSubscription { get; set; }
}