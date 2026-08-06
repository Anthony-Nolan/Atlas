using System;
using System.Collections.Generic;

namespace Atlas.MatchPrediction.ExternalInterface.Models;

/// <summary>
/// Message published to the <c>parallel-match-prediction-results</c> Service Bus topic by the ACA Worker.
/// Session ID is set to <see cref="SearchIdentifier"/> so the aggregator can hold an exclusive lock per search.
/// When <see cref="IsSuccessful"/> is <c>false</c> the batch failed; <see cref="MatchPredictionResultLocation"/>
/// will be <c>null</c> and <see cref="FailureMessage"/>/<see cref="FailureException"/> will be populated.
/// </summary>
public class ParallelMatchPredictionBatchResult
{
    public Guid SearchIdentifier { get; set; }

    public Guid? RepeatSearchIdentifier { get; set; }

    /// <summary>Whether the Worker processed this batch successfully.</summary>
    public bool IsSuccessful { get; set; } = true;

    /// <summary>
    /// Blob filename of the single file holding this batch's donor → match probability result map. Populated only
    /// when <see cref="IsSuccessful"/> is <c>true</c> (<c>null</c> for a batch that contained no donors).
    /// </summary>
    public string MatchPredictionResultLocation { get; set; }

    /// <summary>Id of the parent <c>ParallelMatchPredictionRun</c> row.</summary>
    public int ParallelRunId { get; set; }

    /// <summary>Id of the <c>ParallelMatchPredictionBatch</c> row this result belongs to; the aggregator's persistence key.</summary>
    public int BatchId { get; set; }

    /// <summary>Sequence number of this batch within the run. Retained for logging and ordering.</summary>
    public int BatchSequenceNumber { get; set; }

    /// <summary>
    /// Post-truncation patient imputed genotype count. Identical for every batch in a run (one patient per search),
    /// carried on each batch so its row is self-describing. Populated only when <see cref="IsSuccessful"/> is
    /// <c>true</c>; <c>null</c> for a batch that contained no donors (no patient imputation is performed), and
    /// <c>0</c> only when the patient phenotype was unrepresented.
    /// </summary>
    public int? PatientGenotypeCount { get; set; }

    /// <summary>
    /// Post-truncation imputed genotype count per donor id, covering every donor in the batch. Populated only when
    /// <see cref="IsSuccessful"/> is <c>true</c>. Persisted as a JSON-serialised donorId → count map on the batch row.
    /// </summary>
    public Dictionary<int, int> DonorGenotypeCounts { get; set; }

    /// <summary>
    /// Human-readable failure message. Populated only when <see cref="IsSuccessful"/> is <c>false</c>.
    /// </summary>
    public string FailureMessage { get; set; }

    /// <summary>
    /// Full exception string (type, message and stack trace). Populated only when <see cref="IsSuccessful"/> is <c>false</c>.
    /// </summary>
    public string FailureException { get; set; }
}