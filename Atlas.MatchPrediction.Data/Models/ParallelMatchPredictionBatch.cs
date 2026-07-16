using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
    public const int FailureMessageMaxLength = 1024;

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
    /// UTC time at which the batch first reached a terminal result state: the time its result (success or failure)
    /// was received, or the time the batch was marked <see cref="ParallelMatchPredictionBatchStatus.Failed"/>
    /// because its request message could not be dispatched. <c>null</c> until then.
    /// </summary>
    public DateTime? ResultReceivedTimeUtc { get; set; }

    /// <summary>
    /// Blob filename of the single file holding this batch's donor → MPA result map. <c>null</c> until a successful
    /// result arrives (or when the batch had no donors).
    /// </summary>
    [MaxLength(1024)]
    public string ResultLocation { get; set; }

    /// <summary>
    /// Human-readable failure message from the Worker exception.
    /// Populated only when <see cref="BatchStatus"/> is <see cref="ParallelMatchPredictionBatchStatus.Failed"/>.
    /// </summary>
    [MaxLength(FailureMessageMaxLength)]
    public string FailureMessage { get; set; }

    /// <summary>
    /// Full exception string (type, message and stack trace) from the Worker.
    /// Populated only when <see cref="BatchStatus"/> is <see cref="ParallelMatchPredictionBatchStatus.Failed"/>.
    /// </summary>
    [MaxLength]
    public string FailureException { get; set; }
}