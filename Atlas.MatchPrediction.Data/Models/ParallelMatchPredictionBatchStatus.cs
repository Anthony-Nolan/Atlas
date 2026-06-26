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

    /// <summary>
    /// The batch was still <see cref="Requested"/> when its run was abandoned: no result (success or failure)
    /// ever arrived within the configured timeout (the request message was lost, the Worker failed without
    /// publishing a result, or the result-send itself failed). The row is retained for research until the
    /// run is cleaned up. A late result arriving before cleanup moves the batch on to
    /// <see cref="ResultsReceived"/> or <see cref="Failed"/>.
    /// </summary>
    Abandoned,
}

