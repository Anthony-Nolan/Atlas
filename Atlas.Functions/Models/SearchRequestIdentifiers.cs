namespace Atlas.Functions.Models;

/// <summary>
/// Identifies a search request (and optionally its originating repeat search) throughout orchestration.
/// This is distinct from <see cref="FailureNotificationRequestInfo"/>, which is only constructed
/// at the point of failure and carries failure-specific details.
/// </summary>
public class SearchRequestIdentifiers
{
    public string SearchRequestId { get; set; }

    /// <summary>
    /// Only set for repeat searches. When present, <see cref="SearchRequestId"/> holds
    /// the original search ID and this holds the repeat search ID.
    /// </summary>
    public string RepeatSearchRequestId { get; set; }
}

