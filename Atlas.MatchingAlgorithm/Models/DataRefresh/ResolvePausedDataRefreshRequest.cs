#nullable enable

using Atlas.MatchingAlgorithm.Data.Persistent.Models;

namespace Atlas.MatchingAlgorithm.Models.DataRefresh;

/// <summary>
/// The body of the <see cref="EndpointName"/> endpoint of the data refresh function app: a manual decision for a data
/// refresh that is paused, because more donors failed in the donor genotype precomputation stage than the threshold allows.
/// </summary>
public class ResolvePausedDataRefreshRequest
{
    /// <summary>The name of the function, and the last part of its route (<c>/api/ResolvePausedDataRefresh</c>).</summary>
    public const string EndpointName = "ResolvePausedDataRefresh";

    /// <summary>The id of the paused data refresh record.</summary>
    public int DataRefreshRecordId { get; set; }

    /// <summary>Required. The name of the decision, for example <c>RetryFailedBatches</c>.</summary>
    public DataRefreshManualDecision? Decision { get; set; }

    /// <summary>
    /// Optional. Who took the decision. It goes into the support comments of the record, and into the telemetry event.
    /// </summary>
    public string? DecidedBy { get; set; }
}
