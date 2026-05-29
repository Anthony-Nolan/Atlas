using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Atlas.MatchPrediction.Data.Models;

/// <summary>
/// One row per batch result received from the ACA Worker for a given parallel run.
/// Idempotency is enforced by the unique <c>(RunId, BatchSequenceNumber)</c> index — duplicate
/// Service Bus deliveries simply fail the insert and are ignored by the repository.
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
    /// Together with <see cref="RunId"/> forms the idempotency key.
    /// </summary>
    public int BatchSequenceNumber { get; set; }

    public DateTime ReceivedTimeUtc { get; set; }

    /// <summary>
    /// JSON-serialised <c>IReadOnlyDictionary&lt;int, string&gt;</c> mapping Atlas donor id to
    /// the blob filename (relative to the match-prediction-results container) holding that
    /// donor's per-batch MPA result.
    /// </summary>
    [Required]
    [MaxLength]
    public string ResultLocationsJson { get; set; }
}