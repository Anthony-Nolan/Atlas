using System;
using System.Collections.Generic;

namespace Atlas.MatchPrediction.ExternalInterface.Models;

/// <summary>
/// Message published to the <c>parallel-match-prediction-results</c> Service Bus topic by the ACA Worker.
/// Session ID is set to <see cref="SearchIdentifier"/> so the aggregator can hold an exclusive lock per search.
/// When <see cref="IsSuccessful"/> is <c>false</c> the batch failed; <see cref="MatchPredictionResultLocations"/>
/// will be empty and <see cref="FailureMessage"/>/<see cref="FailureException"/> will be populated.
/// </summary>
public class ParallelMatchPredictionBatchResult
{
    public Guid SearchIdentifier { get; set; }

    public Guid? RepeatSearchIdentifier { get; set; }

    /// <summary>Whether the Worker processed this batch successfully.</summary>
    public bool IsSuccessful { get; set; } = true;

    /// <summary>
    /// Keyed by Atlas donor ID; values are blob filenames (relative to the match-prediction-results container)
    /// where per-donor match probability results can be found.
    /// Populated only when <see cref="IsSuccessful"/> is <c>true</c>.
    /// </summary>
    public IReadOnlyDictionary<int, string> MatchPredictionResultLocations { get; set; }

    /// <summary>Id of the parent <c>ParallelMatchPredictionRun</c> row.</summary>
    public int ParallelRunId { get; set; }

    /// <summary>
    /// Sequence number of this batch as assigned by the orchestrator. Together with <see cref="ParallelRunId"/>
    /// forms the idempotency key used by the aggregator.
    /// </summary>
    public int BatchSequenceNumber { get; set; }

    /// <summary>
    /// Human-readable failure message. Populated only when <see cref="IsSuccessful"/> is <c>false</c>.
    /// </summary>
    public string FailureMessage { get; set; }

    /// <summary>
    /// Full exception string (type, message and stack trace). Populated only when <see cref="IsSuccessful"/> is <c>false</c>.
    /// </summary>
    public string FailureException { get; set; }
}