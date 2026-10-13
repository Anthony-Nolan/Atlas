using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Atlas.MatchPrediction.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Atlas.MatchPrediction.Data.Models;

/// <summary>
/// Parent record for one parallel match-prediction run dispatched to the ACA Worker.
/// One row per <c>SearchRequest</c> on the parallel path. Survives the batch clean-up so that
/// historic searches remain visible even after their per-batch detail rows have been purged.
/// </summary>
[Index(nameof(IsCleanedUp), nameof(MatchPredictionRunInitiatedUtc))]
public class ParallelMatchPredictionRun
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// The originating search request identifier.
    /// For repeat searches this is the <c>SearchIdentifier</c> (i.e., the original search guid), not the repeat search id.
    /// </summary>
    public Guid SearchIdentifier { get; set; }

    public bool IsRepeatSearch { get; set; }

    /// <summary>Non-null only when <see cref="IsRepeatSearch"/> is <c>true</c>.</summary>
    public Guid? RepeatSearchIdentifier { get; set; }

    // ── Fields sourced from MatchingResultsNotification ──────────────────────────

    /// <summary>Blob filename of the matching-results summary file (used by DownloadSummary).</summary>
    [Required]
    [MaxLength(128)]
    public string ResultsFileName { get; set; }

    /// <summary>Whether matching results were written as multiple batched blobs.</summary>
    public bool ResultsBatched { get; set; }

    /// <summary>Blob folder that contains batched result files; non-null only when <see cref="ResultsBatched"/> is <c>true</c>.</summary>
    [MaxLength(36)]
    public string BatchFolderName { get; set; }

    /// <summary>
    /// Elapsed time reported by the matching algorithm.
    /// Persisted as a SQL <c>bigint</c> (ticks) via a value converter to avoid the ~24-hour ceiling
    /// of the SQL <c>time</c> type.
    /// </summary>
    public TimeSpan MatchingAlgorithmElapsedTime { get; set; }

    // ── Orchestration context ─────────────────────────────────────────────────────

    /// <summary>UTC time at which the durable orchestration was initiated (used for performance tracking).</summary>
    public DateTime SearchInitiatedTimeUtc { get; set; }

    /// <summary>Total number of batches dispatched. Matches the number of pre-created <see cref="ParallelMatchPredictionBatch"/> rows.</summary>
    public int TotalBatchCount { get; set; }

    /// <summary>UTC time at which this parallel match-prediction run was created (i.e. batches pre-created and messages about to be dispatched).</summary>
    public DateTime MatchPredictionRunInitiatedUtc { get; set; }

    // ── Status ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Current lifecycle status of this run. Stored as a string for readability.
    /// See <see cref="ParallelMatchPredictionRunStatus"/> for the valid values.
    /// </summary>
    [MaxLength(32)]
    public ParallelMatchPredictionRunStatus Status { get; set; }

    /// <summary>UTC time at which <see cref="Status"/> was last updated.</summary>
    public DateTime? StatusDateUtc { get; set; }

    // ── Finalisation lease ────────────────────────────────────────────────────────

    /// <summary>
    /// The id of the finalisation-function invocation that has claimed this run for processing.
    /// <see cref="IParallelMatchPredictionRepository.TryClaimFinalisationLease"/>
    /// <see cref="IParallelMatchPredictionRepository.MarkRunFinalised"/>
    /// <see cref="IParallelMatchPredictionRepository.MarkRunFailed"/>
    /// </summary>
    public Guid? FinalisationLeaseOwner { get; set; }

    // ── Completion ────────────────────────────────────────────────────────────────

    /// <summary>
    /// UTC time at which final persistence <em>successfully completed</em>.
    /// <c>null</c> until the entire persistence pipeline has finished — set only as the very last step.
    /// Used both for the "find complete runs" query and for cleanup eligibility.
    /// </summary>
    public DateTime? FinalisedTimeUtc { get; set; }

    /// <summary>
    /// Whether this run completed successfully. <c>null</c> until every batch has been processed
    /// (<see cref="ParallelMatchPredictionBatchStatus.ResultsReceived"/> or
    /// <see cref="ParallelMatchPredictionBatchStatus.Failed"/>); <c>true</c> when all succeeded, <c>false</c> when
    /// any failed — including a dispatch failure (see <see cref="ParallelMatchPredictionRunStatus"/>).
    /// </summary>
    public bool? IsSuccessful { get; set; }

    /// <summary>
    /// Whether this run's per-batch detail rows have been purged by the retention clean-up.
    /// Tracked independently of <see cref="Status"/> so that a run keeps its outcome status
    /// (e.g. <see cref="ParallelMatchPredictionRunStatus.Finalised"/> or a failed/abandoned state)
    /// while still being recorded as cleaned up. 
    /// </summary>
    public bool IsCleanedUp { get; set; }

    public ICollection<ParallelMatchPredictionBatch> Batches { get; set; }
}