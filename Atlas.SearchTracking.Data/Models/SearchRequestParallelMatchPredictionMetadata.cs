using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Atlas.SearchTracking.Data.Models;

/// <summary>
/// Stores metadata about a parallel match-prediction run so the aggregator can determine when
/// all batches have been received and final persistence can be triggered.
/// </summary>
public class SearchRequestParallelMatchPredictionMetadata
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// The originating search request identifier
    /// For repeat searches this is the <c>SearchIdentifier</c> (i.e., the original search guid), not the repeat search id.
    /// </summary>
    public required Guid SearchIdentifier { get; set; }

    public bool IsRepeatSearch { get; set; }

    /// <summary>Non-null only when <see cref="IsRepeatSearch"/> is <c>true</c>.</summary>
    public Guid? RepeatSearchIdentifier { get; set; }

    // ── Fields sourced from MatchingResultsNotification ──────────────────────────

    /// <summary>Blob filename of the matching-results summary file (used by DownloadSummary).</summary>
    [MaxLength(128)]
    public required string ResultsFileName { get; set; }

    /// <summary>Whether matching results were written as multiple batched blobs.</summary>
    public bool ResultsBatched { get; set; }

    /// <summary>Blob folder that contains batched result files; non-null only when <see cref="ResultsBatched"/> is <c>true</c>.</summary>
    [MaxLength(36)]
    public string? BatchFolderName { get; set; }

    /// <summary>
    /// Elapsed time reported by the matching algorithm.
    /// Persisted as a SQL <c>bigint</c> (ticks) via a value converter to avoid the ~24-hour ceiling
    /// of the SQL <c>time</c> type.
    /// </summary>
    public TimeSpan MatchingAlgorithmElapsedTime { get; set; }

    // ── Orchestration context ─────────────────────────────────────────────────────

    /// <summary>UTC time at which the durable orchestration was initiated (used for performance tracking).</summary>
    [Required]
    public DateTime SearchInitiatedTimeUtc { get; set; }

    /// <summary>Total number of batches that were dispatched. This is duplicated here for convenience, otherwise following a link
    /// to the original search request would require several separate join operations.</summary>
    public int TotalBatchCount { get; set; }

    /// <summary>How many batch results have been received so far. Default 0.</summary>
    public int ProcessedBatchCount { get; set; }

    public ICollection<SearchRequestParallelMatchPredictionResultLocation> ResultLocations { get; set; }
}