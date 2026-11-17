namespace Atlas.RepeatSearch.Settings;

public class RepeatSearchSettings
{
    /// <summary>
    /// When the result set of the original search is not stored yet, the repeat search request is published again with this delay,
    /// to check again later.
    /// </summary>
    public int OriginalResultsWaitIntervalSeconds { get; set; }

    /// <summary>
    /// How long a repeat search waits for the result set of the original search to be stored, from the time of its first request.
    /// After this time, the repeat search fails with a validation error.
    /// </summary>
    public int MaxWaitForOriginalResultsMinutes { get; set; }
}
