namespace Atlas.MatchPrediction.Data.Models;

/// <summary>
/// Lifecycle status of a parallel match-prediction run.
/// </summary>
public enum ParallelMatchPredictionRunStatus
{
    /// <summary>
    /// The run has been created and batches have been dispatched; results are still being received.
    /// Also covers the brief dispatch-failed state: when publishing the batch requests fails, the run keeps this
    /// status (with <see cref="ParallelMatchPredictionRun.IsSuccessful"/> already <c>false</c> and every batch
    /// marked <see cref="ParallelMatchPredictionBatchStatus.Failed"/>) until the finaliser picks it up and
    /// transitions it to <see cref="FailedDuringBatchProcessing"/>.
    /// </summary>
    Running,

    /// <summary>
    /// The run was fully finalised: all batch results were received and the persistence pipeline completed
    /// successfully.
    /// </summary>
    Finalised,

    /// <summary>
    /// The persistence pipeline threw while finalising this run. The run is terminal — it will not be
    /// re-picked by the finaliser timer. Per-batch rows remain in place for audit/debugging.
    /// </summary>
    FailedDuringCompletion,

    /// <summary>
    /// At least one ACA Worker batch reported a failure. 
    /// </summary>
    FailedDuringBatchProcessing,

    /// <summary>
    /// One or more batches never returned a result within the configured timeout, so the run was abandoned:
    /// the missing batches were marked <see cref="ParallelMatchPredictionBatchStatus.Abandoned"/> and a
    /// failure notification was sent downstream. Per-batch rows are retained for research until cleanup.
    /// </summary>
    Abandoned,
}

