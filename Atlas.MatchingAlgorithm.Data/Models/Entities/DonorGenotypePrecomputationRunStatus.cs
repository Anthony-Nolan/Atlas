#nullable enable

namespace Atlas.MatchingAlgorithm.Data.Models.Entities;

/// <summary>
/// Where a <see cref="DonorGenotypePrecomputationRun"/> is in the donor genotype precomputation stage of the data
/// refresh. Persisted as its member name (string), like <see cref="AllowedLociKey"/>.
/// </summary>
public enum DonorGenotypePrecomputationRunStatus
{
    /// <summary>
    /// The stage is building the groups and the batches. A stage that stops here builds them again from the start.
    /// </summary>
    Building = 1,

    /// <summary>
    /// The batches exist, and the workers process them. A worker acts on a batch only while its run has this status.
    /// </summary>
    Running = 2,

    /// <summary>Every batch has results, and no group failed.</summary>
    Completed = 3,

    /// <summary>
    /// Every batch is terminal, but at least one group or one batch failed. The donors of those groups have no rows.
    /// </summary>
    CompletedWithFailures = 4,

    /// <summary>The data refresh failed and stopped the run. The workers skip its messages.</summary>
    Cancelled = 5
}
