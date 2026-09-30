using System;
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
    string RegistryCode,
    string EthnicityCode,
    PhenotypeInfo<string> HlaTyping);

/// <summary>
/// What a worker found for one group: a stored value, or a failure. Exactly one of the two is set.
/// </summary>
public sealed record DonorGenotypePrecomputationGroupOutcome(int GroupId, int? SubjectGenotypeSetValueId, string FailureMessage)
{
    public static DonorGenotypePrecomputationGroupOutcome Stored(int groupId, int subjectGenotypeSetValueId) =>
        new(groupId, subjectGenotypeSetValueId, null);

    public static DonorGenotypePrecomputationGroupOutcome Failed(int groupId, string failureMessage) =>
        new(groupId, null, failureMessage ?? string.Empty);
}

/// <summary>Why a worker stopped a batch.</summary>
public sealed record DonorGenotypePrecomputationBatchFailure(
    string FailureMessage,
    string FailureException,
    int FailedGroupCount);

/// <summary>A batch that a sweep moved, as it was just before the move.</summary>
public sealed record SweptDonorGenotypePrecomputationBatch(
    int BatchId,
    DonorGenotypePrecomputationBatchStatus PreviousStatus,
    int RetryCount,
    string FailureMessage);
