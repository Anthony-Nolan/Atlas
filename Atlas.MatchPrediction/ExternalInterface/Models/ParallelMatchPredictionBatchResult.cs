using System;
using System.Collections.Generic;

namespace Atlas.MatchPrediction.ExternalInterface.Models;

/// <summary>
/// Message published to the <c>parallel-match-prediction-results</c> Service Bus topic by the ACA Worker.
/// Session ID is set to <see cref="SearchIdentifier"/> so the aggregator can hold an exclusive lock per search.
/// </summary>
public class ParallelMatchPredictionBatchResult
{
    public Guid SearchIdentifier { get; set; }

    public Guid? RepeatSearchIdentifier { get; set; }

    /// <summary>
    /// Keyed by Atlas donor ID; values are blob filenames (relative to the match-prediction-results container)
    /// where per-donor match probability results can be found.
    /// </summary>
    public IReadOnlyDictionary<int, string> MatchPredictionResultLocations { get; set; }

    /// <summary>Id of the parent <c>ParallelMatchPredictionRun</c> row.</summary>
    public int ParallelRunId { get; set; }

    /// <summary>
    /// Sequence number of this batch as assigned by the orchestrator. Together with <see cref="ParallelRunId"/>
    /// forms the idempotency key used by the aggregator.
    /// </summary>
    public int BatchSequenceNumber { get; set; }
}