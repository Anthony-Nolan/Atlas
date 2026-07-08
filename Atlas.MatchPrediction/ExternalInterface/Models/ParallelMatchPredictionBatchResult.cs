using System;

namespace Atlas.MatchPrediction.ExternalInterface.Models;

/// <summary>
/// Message published to the <c>parallel-match-prediction-results</c> Service Bus topic by the ACA Worker.
/// Session ID is set to <see cref="SearchIdentifier"/> so the aggregator can hold an exclusive lock per search.
/// When <see cref="IsSuccessful"/> is <c>false</c> the batch failed; <see cref="MatchPredictionResultLocation"/>
/// will be <c>null</c> and <see cref="FailureMessage"/>/<see cref="FailureException"/> will be populated.
/// </summary>
public class ParallelMatchPredictionBatchResult
{
    public Guid SearchIdentifier { get; set; }

    public Guid? RepeatSearchIdentifier { get; set; }

    /// <summary>Whether the Worker processed this batch successfully.</summary>
    public bool IsSuccessful { get; set; } = true;

    /// <summary>
    /// Blob filename of the single file holding this batch's donor → match probability result map. Populated only
    /// when <see cref="IsSuccessful"/> is <c>true</c> (<c>null</c> for a batch that contained no donors).
    /// </summary>
    public string MatchPredictionResultLocation { get; set; }

    /// <summary>Id of the parent <c>ParallelMatchPredictionRun</c> row.</summary>
    public int ParallelRunId { get; set; }

    /// <summary>Id of the <c>ParallelMatchPredictionBatch</c> row this result belongs to; the aggregator's persistence key.</summary>
    public int BatchId { get; set; }

    /// <summary>Sequence number of this batch within the run. Retained for logging and ordering.</summary>
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