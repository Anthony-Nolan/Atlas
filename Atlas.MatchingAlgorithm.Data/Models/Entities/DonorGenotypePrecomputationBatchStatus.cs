#nullable enable

namespace Atlas.MatchingAlgorithm.Data.Models.Entities;

/// <summary>
/// The state of one <see cref="DonorGenotypePrecomputationBatch"/>. Persisted as its member name (string), like
/// <see cref="AllowedLociKey"/>.
///
/// <para>
/// <see cref="ResultsReceived"/> and <see cref="PermanentlyFailed"/> are the terminal states: the stage waits until every
/// batch of the run is in one of them. Every change of state is a compare-and-swap (<c>UPDATE ... WHERE Status IN (...)</c>),
/// so two writers that race for the same change cannot both make it.
/// </para>
/// </summary>
public enum DonorGenotypePrecomputationBatchStatus
{
    /// <summary>No message is out for the batch: it is new, or it was sent back after a retryable failure.</summary>
    Pending = 1,

    /// <summary>Its message is on the requests topic.</summary>
    Requested = 2,

    /// <summary>A worker claimed the batch, and holds its lease.</summary>
    InProgress = 3,

    /// <summary>Terminal. Every group of the batch has its value or its failure, and the donor rows are written.</summary>
    ResultsReceived = 4,

    /// <summary>
    /// The worker stopped the batch. The requeue sweep sends it again, or gives up on it when it has no retries left.
    /// </summary>
    Failed = 5,

    /// <summary>The lease expired, or the message was dead-lettered: no worker is on the batch.</summary>
    Abandoned = 6,

    /// <summary>Terminal. The batch failed or was abandoned, and it has no retries left.</summary>
    PermanentlyFailed = 7
}
