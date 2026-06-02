using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Atlas.MatchPrediction.Data.Models;

/// <summary>
/// One row per batch in a parallel match-prediction run, pre-created before any batch messages are dispatched.
/// Idempotency for result recording is enforced by the <see cref="BatchStatus"/> field — once a result
/// is received (or a failure recorded), the row is updated atomically; duplicate Service Bus deliveries are detected and ignored.
/// The unique <c>(RunId, BatchSequenceNumber)</c> index is used to find the correct row to update.
/// Rows are purged by the clean-up timer after the parent run is finalised (the parent row itself is kept).
/// </summary>
[Index(nameof(RunId), nameof(BatchSequenceNumber), IsUnique = true)]
public class ParallelMatchPredictionBatch
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Run))]
    public int RunId { get; set; }

    public ParallelMatchPredictionRun Run { get; set; }

    /// <summary>
    /// Zero-based sequence number assigned by the orchestrator when the batch is dispatched.
    /// Together with <see cref="RunId"/> forms the idempotency key used to locate this row when a result arrives.
    /// The same sequence number is embedded in the <c>ParallelMatchPredictionBatchRequest</c> message and
    /// echoed back on the <c>ParallelMatchPredictionBatchResult</c> message so the aggregator can find this row.
    /// </summary>
    public int BatchSequenceNumber { get; set; }

    /// <summary>
    /// Current lifecycle status of this batch.
    /// Starts as <see cref="ParallelMatchPredictionBatchStatus.Requested"/> and transitions to
    /// <see cref="ParallelMatchPredictionBatchStatus.ResultsReceived"/> or
    /// <see cref="ParallelMatchPredictionBatchStatus.Failed"/> when the Worker publishes a result message.
    /// </summary>
    [MaxLength(32)]
    public ParallelMatchPredictionBatchStatus BatchStatus { get; set; } = ParallelMatchPredictionBatchStatus.Requested;

    /// <summary>UTC time at which the result (success or failure) was first received. <c>null</c> until a result arrives.</summary>
    public DateTime? ResultReceivedTimeUtc { get; set; }

    /// <summary>
    /// JSON-serialised <c>IReadOnlyDictionary&lt;int, string&gt;</c> mapping Atlas donor id to
    /// the blob filename (relative to the match-prediction-results container) holding that
    /// donor's per-batch MPA result. <c>null</c> until a successful result arrives.
    /// </summary>
    [MaxLength]
    public string ResultLocationJson { get; set; }

    /// <summary>
    /// Human-readable failure message from the Worker exception.
    /// Populated only when <see cref="BatchStatus"/> is <see cref="ParallelMatchPredictionBatchStatus.Failed"/>.
    /// </summary>
    [MaxLength(1024)]
    public string FailureMessage { get; set; }

    /// <summary>
    /// Full exception string (type, message and stack trace) from the Worker.
    /// Populated only when <see cref="BatchStatus"/> is <see cref="ParallelMatchPredictionBatchStatus.Failed"/>.
    /// </summary>
    [MaxLength]
    public string FailureException { get; set; }
}