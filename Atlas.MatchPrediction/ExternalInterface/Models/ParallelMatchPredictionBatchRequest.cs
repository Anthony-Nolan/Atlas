namespace Atlas.MatchPrediction.ExternalInterface.Models;

/// <summary>
/// Message published to the <c>parallel-match-prediction-requests</c> Service Bus topic by the orchestrator.
/// The ACA Worker consumes this message, processes the batch blob, and publishes results to
/// <c>parallel-match-prediction-results</c> with <c>SessionId = SearchRequestId</c>.
/// </summary>
public class ParallelMatchPredictionBatchRequest
{
    /// <summary>Path to the batch blob in the <c>match-prediction-requests</c> container (a <c>MultipleDonorMatchProbabilityInput</c>).</summary>
    public string BlobLocation { get; set; }

    /// <summary>Originating search request identifier. Used by the Worker as the Service Bus session ID on result messages.</summary>
    public string SearchRequestId { get; set; }

    public bool IsRepeatSearch { get; set; }

    /// <summary>Non-null only when <see cref="IsRepeatSearch"/> is <c>true</c>.</summary>
    public string RepeatSearchRequestId { get; set; }

    /// <summary>Id of the parent <c>ParallelMatchPredictionRun</c> row created by the orchestrator before dispatch.</summary>
    public int ParallelRunId { get; set; }

    /// <summary>
    /// Id of the pre-created <c>ParallelMatchPredictionBatch</c> row. The Worker names the output blob after it and
    /// echoes it back on the result; the aggregator keys per-batch persistence on it.
    /// </summary>
    public int BatchId { get; set; }

    /// <summary>Zero-based sequence number of this batch within the run. Retained for logging and ordering.</summary>
    public int BatchSequenceNumber { get; set; }
}