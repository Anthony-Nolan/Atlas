#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;

namespace Atlas.MatchingAlgorithm.Data.Models.Precompute;

/// <summary>
/// A worker's request to take one batch of the donor genotype precomputation stage of the data refresh.
/// </summary>
/// <param name="DataRefreshRecordId">From the message. The claim fails unless the run belongs to this record.</param>
/// <param name="LeaseOwner">New for each claim, so a worker that lost its batch cannot complete it.</param>
/// <param name="LeaseDuration">How long the claim holds before a sweep can mark the batch abandoned.</param>
/// <param name="IsRedelivery">
/// True when Service Bus delivers the message again. A redelivered message can take a batch that is still in progress:
/// its first worker stopped without a result, and the message came back without a wait for the lease.
/// </param>
public sealed record DonorGenotypePrecomputationBatchClaim(
    int DataRefreshRecordId,
    int RunId,
    int BatchId,
    Guid LeaseOwner,
    TimeSpan LeaseDuration,
    bool IsRedelivery);

/// <summary>A batch that a worker claimed, with the parts of its batch and run rows that the worker needs.</summary>
public sealed record ClaimedDonorGenotypePrecomputationBatch(
    int BatchId,
    int RunId,
    int FirstGroupId,
    int LastGroupId,
    int RetryCount,
    string HlaNomenclatureVersion);

/// <summary>
/// A group that has no value yet, with the typing and the codes of its representative donor.
/// </summary>
/// <param name="HlaTyping">Null when the representative donor is not in <c>Donors</c>: the group cannot be computed.</param>
public sealed record DonorGenotypePrecomputationGroupToCompute(
    int GroupId,
    AllowedLociKey AllowedLociKey,
    int RepresentativeDonorId,
    string? RegistryCode,
    string? EthnicityCode,
    PhenotypeInfo<string?>? HlaTyping);

/// <summary>
/// What a worker found for one group: a stored value, or a failure. Exactly one of the two is set.
/// </summary>
public sealed record DonorGenotypePrecomputationGroupOutcome(int GroupId, int? SubjectGenotypeSetValueId, string? FailureMessage)
{
    public static DonorGenotypePrecomputationGroupOutcome Stored(int groupId, int subjectGenotypeSetValueId) =>
        new(groupId, subjectGenotypeSetValueId, null);

    public static DonorGenotypePrecomputationGroupOutcome Failed(int groupId, string failureMessage) =>
        new(groupId, null, failureMessage);
}

/// <summary>Why a worker stopped a batch.</summary>
/// <param name="FailureException">Null when no exception caused the failure: too many groups failed.</param>
public sealed record DonorGenotypePrecomputationBatchFailure(
    string FailureMessage,
    string? FailureException,
    int FailedGroupCount);

/// <summary>A batch that a sweep moved, as it was just before the move.</summary>
public sealed record SweptDonorGenotypePrecomputationBatch(
    int BatchId,
    DonorGenotypePrecomputationBatchStatus PreviousStatus,
    int RetryCount,
    string? FailureMessage);

/// <summary>A <see cref="DonorGenotypePrecomputationBatchStatus.Pending"/> batch, as a dispatch read it.</summary>
/// <param name="RetryCount">
/// The retry count when the dispatch read the batch. The dispatch moves the batch to requested only while the count is
/// the same: a batch that failed and was sent back in the meantime has a higher count, and its new message is the
/// sweep's to send.
/// </param>
public sealed record PendingDonorGenotypePrecomputationBatch(int BatchId, int RetryCount);

/// <summary>
/// Which <see cref="DonorGenotypePrecomputationBatchStatus.Pending"/> batches a dispatch sends. Two senders share the
/// pending batches, and they must not send the same ones.
/// </summary>
public enum PendingBatchSelection
{
    /// <summary>
    /// Every pending batch: the first dispatch of the stage, and a stage that starts again, also after a manual retry.
    /// </summary>
    All,

    /// <summary>Only the batches that a sweep sent back after a failure or an abandonment: a retry count above 0.</summary>
    Requeued
}

/// <summary>The batches of a run per status.</summary>
/// <param name="FailedGroupCount">
/// The failed groups of the <see cref="DonorGenotypePrecomputationBatchStatus.ResultsReceived"/> batches. Their donors
/// have no rows.
/// </param>
public sealed record DonorGenotypePrecomputationBatchCounts(
    IReadOnlyDictionary<DonorGenotypePrecomputationBatchStatus, int> BatchCountByStatus,
    int FailedGroupCount)
{
    public int TotalBatchCount => BatchCountByStatus.Values.Sum();

    /// <summary>The batches that are done, with results or not.</summary>
    public int TerminalBatchCount =>
        CountOf(DonorGenotypePrecomputationBatchStatus.ResultsReceived) + CountOf(DonorGenotypePrecomputationBatchStatus.PermanentlyFailed);

    public int CountOf(DonorGenotypePrecomputationBatchStatus status) => BatchCountByStatus.GetValueOrDefault(status);
}

/// <summary>
/// What failed in a run: the input of the failure threshold of the stage, and of its alerts.
/// </summary>
/// <param name="PermanentlyFailedBatchCount">The batches that gave up.</param>
/// <param name="FailedGroupCount">
/// The groups with no value in the <see cref="DonorGenotypePrecomputationBatchStatus.ResultsReceived"/> batches: a known
/// permanent error, such as a bad typing, fails its group and not its batch. The groups of the permanently failed batches
/// are not in this count.
/// </param>
/// <param name="FailedDonorCount">
/// The donors that have no row for at least one <see cref="AllowedLociKey"/>: every donor of a permanently failed batch,
/// and every donor of a failed group. Each donor counts once. A batch that stopped part way can have values but no donor
/// rows, so a permanently failed batch counts all its donors.
/// </param>
/// <param name="TotalDonorCount">The donors of the run.</param>
/// <param name="Samples">Up to ten failures, one per distinct message: the failures of whole batches first.</param>
public sealed record DonorGenotypePrecomputationFailureSummary(
    int PermanentlyFailedBatchCount,
    int FailedGroupCount,
    int FailedDonorCount,
    int TotalDonorCount,
    IReadOnlyList<DonorGenotypePrecomputationFailureSample> Samples)
{
    public bool HasFailures => PermanentlyFailedBatchCount > 0 || FailedGroupCount > 0;

    /// <summary>0 for a run with no donors.</summary>
    public double FailedDonorFraction => TotalDonorCount == 0 ? 0 : (double)FailedDonorCount / TotalDonorCount;

    /// <summary>
    /// True when more donors failed than the threshold allows. A threshold of 0 allows no failed donor.
    /// </summary>
    /// <param name="maxFailedDonorFraction">The largest failed-donor fraction that the stage accepts, from 0 to 1.</param>
    public bool IsAboveThreshold(double maxFailedDonorFraction) => FailedDonorFraction > maxFailedDonorFraction;
}

/// <summary>One failure of a run.</summary>
/// <param name="GroupId">Null when the whole batch failed.</param>
public sealed record DonorGenotypePrecomputationFailureSample(int BatchId, int? GroupId, string? FailureMessage);

/// <summary>What the build of a run made, and how long each of its steps took.</summary>
/// <param name="Status">
/// The status that the build gave the run: <see cref="DonorGenotypePrecomputationRunStatus.Running"/>, or
/// <see cref="DonorGenotypePrecomputationRunStatus.Completed"/> when there were no donors, so no batches.
/// </param>
/// <param name="TotalDonorCount">The donors in <c>Donors</c>.</param>
/// <param name="TotalDonorAssignmentCount">The group-donor rows: one for each donor at each <see cref="AllowedLociKey"/>.</param>
/// <param name="Steps">The steps of the build, in order.</param>
public sealed record DonorGenotypePrecomputationBuildResult(
    DonorGenotypePrecomputationRunStatus Status,
    int TotalDonorCount,
    int TotalGroupCount,
    int TotalBatchCount,
    int TotalDonorAssignmentCount,
    IReadOnlyList<DonorGenotypePrecomputationBuildStep> Steps);

/// <summary>One step of the build of a run.</summary>
/// <param name="RowCount">The rows that the step wrote.</param>
public sealed record DonorGenotypePrecomputationBuildStep(string Name, int RowCount, TimeSpan Duration);
