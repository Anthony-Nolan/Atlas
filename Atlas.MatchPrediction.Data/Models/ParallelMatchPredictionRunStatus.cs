namespace Atlas.MatchPrediction.Data.Models;

/// <summary>
/// Lifecycle status of a parallel match-prediction run.
/// </summary>
/// <remarks>
/// This type is the single source of truth for the run lifecycle; members elsewhere point here rather than
/// re-describing these flows.
/// <para>
/// <b>Finalisation lease.</b> A timer sweeps for runs ready to (re-)finalise and claims each via a compare-and-swap
/// on <see cref="ParallelMatchPredictionRun.FinalisationLeaseOwner"/>, so two concurrent invocations can never
/// process the same run. The lease is released again on a non-terminal failure (so the run can be replayed) and
/// left in place once the run reaches a terminal state.
/// </para>
/// <para>
/// <b>Replay / dead-letter recovery.</b> <see cref="Abandoned"/> and <see cref="FailedDuringBatchProcessing"/> are
/// non-terminal: once every batch has a result — late arrivals, or a full dead-letter recovery of the failed
/// batches — the finaliser re-picks the run and drives it to <see cref="Finalised"/>. The
/// <see cref="FailedDuringBatchProcessing"/> replay is deliberately stricter: a failed run has already reported its
/// failure downstream, so it is only re-picked on a full recovery to success, never re-notified while any batch is
/// still failed.
/// </para>
/// <para>
/// <b>Dispatch failure.</b> If publishing the batch-request messages fails, the run is left in <see cref="Running"/>
/// with <see cref="ParallelMatchPredictionRun.IsSuccessful"/> already <c>false</c> and every batch marked
/// <see cref="ParallelMatchPredictionBatchStatus.Failed"/>; leaving it Running with no outstanding batches makes it
/// immediately eligible for the finaliser, which transitions it to <see cref="FailedDuringBatchProcessing"/>.
/// </para>
/// </remarks>
public enum ParallelMatchPredictionRunStatus
{
    /// <summary>
    /// The run has been created and its batches dispatched; results are still being received. Also the run's resting
    /// state immediately after a dispatch failure — see the type-level remarks.
    /// </summary>
    Running,

    /// <summary>
    /// The run was fully finalised: all batch results were received and the persistence pipeline completed
    /// successfully.
    /// </summary>
    Finalised,

    /// <summary>
    /// The persistence pipeline threw while finalising this run. Terminal — the finaliser will not re-pick it;
    /// per-batch rows remain in place for audit/debugging.
    /// </summary>
    FailedDuringCompletion,

    /// <summary>
    /// At least one batch failed — reported by the ACA Worker, or synthesised when dispatch failed. Non-terminal:
    /// see the replay flow in the type-level remarks.
    /// </summary>
    FailedDuringBatchProcessing,

    /// <summary>
    /// One or more batches did not return a result within the configured timeout, so the run was abandoned and a
    /// failure notification sent; the still-pending batches are marked
    /// <see cref="ParallelMatchPredictionBatchStatus.Abandoned"/>. Non-terminal: see the replay flow in the
    /// type-level remarks. Per-batch rows are retained for research until cleanup.
    /// </summary>
    Abandoned,
}

