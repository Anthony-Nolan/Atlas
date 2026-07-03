using Atlas.Client.Models.Search.Results.Matching;

namespace Atlas.Functions.Models;

/// <summary>
/// Parameters passed to the <see cref="DurableFunctions.Search.Activity.SearchActivityFunctions.SendFailureNotification"/> activity function.
/// </summary>
public record SendFailureNotificationParameters
{
    public string SearchRequestId { get; init; }

    /// <summary>Null for non-repeat searches.</summary>
    public string RepeatSearchRequestId { get; init; }

    /// <summary>The orchestration stage at which the failure occurred.</summary>
    public string StageReached { get; init; }

    /// <summary>
    /// Optional free-form detail describing why the search failed (e.g. the match-prediction abandonment reason).
    /// Surfaced to downstream consumers via <see cref="Atlas.Client.Models.Search.Results.SearchFailureInfo.Message"/>.
    /// </summary>
    public string FailureDetail { get; init; }

    /// <summary>
    /// Only populated when the failure originated in the matching algorithm.
    /// </summary>
    public MatchingAlgorithmFailureInfo MatchingAlgorithmFailureInfo { get; init; }
}

