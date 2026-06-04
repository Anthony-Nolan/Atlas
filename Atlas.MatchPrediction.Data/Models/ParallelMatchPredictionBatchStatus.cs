namespace Atlas.MatchPrediction.Data.Models;

/// <summary>
/// Lifecycle status of a single batch within a parallel match-prediction run.
/// </summary>
public enum ParallelMatchPredictionBatchStatus
{
    /// <summary>The batch row has been pre-created and the request message dispatched; awaiting a result.</summary>
    Requested,

    /// <summary>The ACA Worker processed the batch successfully and published result locations.</summary>
    ResultsReceived,

    /// <summary>
    /// The ACA Worker encountered an unrecoverable error while processing this batch.
    /// <see cref="ParallelMatchPredictionBatch.FailureMessage"/> and
    /// <see cref="ParallelMatchPredictionBatch.FailureException"/> capture the details.
    /// </summary>
    Failed,
}

