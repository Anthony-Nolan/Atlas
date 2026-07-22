using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Atlas.Common.Sql;
using Microsoft.EntityFrameworkCore;

namespace Atlas.MatchPrediction.Data.Models;

/// <summary>
/// One row per batch in a parallel match-prediction run, pre-created before any batch messages are dispatched.
/// The row's <see cref="Id"/> is stamped onto the dispatched request and echoed back on the result, so the aggregator
/// locates and updates the row by primary key; idempotency is enforced by <see cref="BatchStatus"/> (duplicate
/// deliveries are ignored). Rows are purged by the clean-up timer after the parent run is finalised.
/// </summary>
[Index(nameof(RunId), nameof(BatchSequenceNumber), IsUnique = true)]
public class ParallelMatchPredictionBatch
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Run))]
    public int RunId { get; set; }

    public ParallelMatchPredictionRun Run { get; set; }

    /// <summary>Zero-based sequence number assigned by the orchestrator. Retained for logging and ordering.</summary>
    public int BatchSequenceNumber { get; set; }

    /// <summary>
    /// Current lifecycle status of this batch.
    /// Starts as <see cref="ParallelMatchPredictionBatchStatus.Requested"/> and transitions to
    /// <see cref="ParallelMatchPredictionBatchStatus.ResultsReceived"/> or
    /// <see cref="ParallelMatchPredictionBatchStatus.Failed"/> when the Worker publishes a result message.
    /// </summary>
    [MaxLength(32)]
    public ParallelMatchPredictionBatchStatus BatchStatus { get; set; } = ParallelMatchPredictionBatchStatus.Requested;

    /// <summary>
    /// UTC time at which <see cref="BatchStatus"/> was last set: stamped on initial creation
    /// (<see cref="ParallelMatchPredictionBatchStatus.Requested"/>) and updated on every subsequent transition,
    /// including when the batch is marked <see cref="ParallelMatchPredictionBatchStatus.Failed"/> because its request
    /// message could not be dispatched. Unlike <see cref="ResultReceivedTimeUtc"/>, this is set whether or not a
    /// Worker result was ever received.
    /// </summary>
    public DateTime? BatchStatusDate { get; set; }

    /// <summary>
    /// UTC time at which the batch's result (success or failure) was received from the Worker. <c>null</c> until then,
    /// and remains <c>null</c> for a batch that never dispatched (a dispatch failure records no result) — use
    /// <see cref="BatchStatusDate"/> for the time such a batch was marked <see cref="ParallelMatchPredictionBatchStatus.Failed"/>.
    /// </summary>
    public DateTime? ResultReceivedTimeUtc { get; set; }

    /// <summary>
    /// Blob filename of the single file holding this batch's donor → MPA result map. <c>null</c> until a successful
    /// result arrives (or when the batch had no donors).
    /// </summary>
    [MaxLength(StringColumnLengths.LongText)]
    public string ResultLocation { get; set; }

    /// <summary>
    /// Human-readable failure message from the Worker exception.
    /// Populated only when <see cref="BatchStatus"/> is <see cref="ParallelMatchPredictionBatchStatus.Failed"/>.
    /// </summary>
    [MaxLength(StringColumnLengths.LongText)]
    public string FailureMessage { get; set; }

    /// <summary>
    /// Full exception string (type, message and stack trace) from the Worker.
    /// Populated only when <see cref="BatchStatus"/> is <see cref="ParallelMatchPredictionBatchStatus.Failed"/>.
    /// </summary>
    [MaxLength]
    public string FailureException { get; set; }
}