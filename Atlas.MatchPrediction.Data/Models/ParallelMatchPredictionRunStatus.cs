namespace Atlas.MatchPrediction.Data.Models;

/// <summary>
/// Lifecycle status of a parallel match-prediction run.
/// </summary>
public enum ParallelMatchPredictionRunStatus
{
    /// <summary>The run has been created and batches have been dispatched; results are still being received.</summary>
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
    /// At least one ACA Worker batch reported a failure. The completion pipeline ran (sending performance
    /// metrics, a failure notification and the tracking event) but no search results were persisted.
    /// </summary>
    FailedDuringBatchProcessing,
}

