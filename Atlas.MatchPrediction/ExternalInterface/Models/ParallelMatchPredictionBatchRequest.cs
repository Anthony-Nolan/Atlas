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

    /// <summary>
    /// Total number of batch messages dispatched for this search request.
    /// The Worker must forward this value on every result message so the aggregator knows when all batches are complete.
    /// </summary>
    public int TotalBatches { get; set; }

    public int ParallelMetadataId { get; set; }
}