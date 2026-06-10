using System;
using Atlas.Client.Models.Search.Results.Matching;

namespace Atlas.Functions.Models;

/// <summary>
/// Parameters passed to the <c>PrepareAndDispatchParallelMatchPredictionBatches</c> activity function.
/// Wraps both the matching results notification and the orchestration start time, which the activity
/// forwards to each <c>ParallelMatchPredictionBatchRequest</c> so the aggregator has complete context.
/// </summary>
public class PrepareAndDispatchParallelMatchPredictionBatchesParameters
{
    public MatchingResultsNotification MatchingResultsNotification { get; set; }

    /// <summary>UTC time at which the durable orchestration started.</summary>
    public DateTime SearchInitiatedTimeUtc { get; set; }
}