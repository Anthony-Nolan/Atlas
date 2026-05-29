using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Atlas.MatchPrediction.Data.Models;

/// <summary>
/// Parent record for one parallel match-prediction run dispatched to the ACA Worker.
/// One row per <c>SearchRequest</c> on the parallel path. Survives the batch clean-up so that
/// historic searches remain visible even after their per-batch detail rows have been purged.
/// </summary>
[Index(nameof(FinalisedTimeUtc))]
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

    /// <summary>Total number of batches dispatched. Used by the finaliser to detect completeness.</summary>
    public int TotalBatchCount { get; set; }

    // ── finalisation lease + completion ────────────────────────────────────────────

    /// <summary>
    /// Identifies the finaliser instance that currently holds the finalisation lease on this run.
    /// Set atomically when a finaliser claims the run; checked again when marking the run finalised so a
    /// stale finaliser (whose lease expired and was re-claimed by another instance) cannot complete it.
    /// </summary>
    public Guid? FinalisationLeaseOwner { get; set; }

    /// <summary>
    /// UTC time at which the current finalisation lease expires. <c>null</c> when no finaliser has ever
    /// claimed the run. A run is claimable when this is <c>null</c> or in the past, which allows a crashed
    /// finaliser's work to be retried once its lease lapses (rather than the run being abandoned).
    /// </summary>
    public DateTime? FinalisationLeaseExpiresUtc { get; set; }

    /// <summary>
    /// UTC time at which final persistence <em>successfully completed</em>.
    /// <c>null</c> until the entire persistence pipeline has finished — set only as the very last step so
    /// that a failure part-way through leaves the run eligible for re-Finalisation once its lease expires.
    /// Used both for the "find complete runs" query and for cleanup eligibility.
    /// </summary>
    public DateTime? FinalisedTimeUtc { get; set; }

    public ICollection<ParallelMatchPredictionBatch> Batches { get; set; }
}