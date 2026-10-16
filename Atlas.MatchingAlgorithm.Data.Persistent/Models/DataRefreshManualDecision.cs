namespace Atlas.MatchingAlgorithm.Data.Persistent.Models;

/// <summary>
/// The decision that support takes for a data refresh that is paused, because more donors failed in the donor genotype
/// precomputation stage than the threshold allows.
/// </summary>
/// <remarks>
/// Stored as the member name. There is no 0 member, so a request with no decision does not give a decision.
/// </remarks>
public enum DataRefreshManualDecision
{
    /// <summary>
    /// Accept the failures. The refresh marks the donor genotype precomputation stage as complete, and continues with the
    /// later stages.
    /// </summary>
    Complete = 1,

    /// <summary>
    /// Compute the failed work again. The refresh scales the database up, and the donor genotype precomputation stage sends
    /// only the failed batches again.
    /// </summary>
    RetryFailedBatches = 2,

    /// <summary>Close the refresh as failed. There is no swap.</summary>
    Fail = 3
}
