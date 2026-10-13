#nullable enable

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Services;
using Dapper;
using Microsoft.Data.SqlClient;
using Batch = Atlas.MatchingAlgorithm.Data.Models.Entities.DonorGenotypePrecomputationBatch;
using BatchStatus = Atlas.MatchingAlgorithm.Data.Models.Entities.DonorGenotypePrecomputationBatchStatus;
using Group = Atlas.MatchingAlgorithm.Data.Models.Entities.DonorGenotypePrecomputationGroup;
using GroupDonor = Atlas.MatchingAlgorithm.Data.Models.Entities.DonorGenotypePrecomputationGroupDonor;
using Outcome = Atlas.MatchingAlgorithm.Data.Models.Precompute.DonorGenotypePrecomputationGroupOutcome;
using Run = Atlas.MatchingAlgorithm.Data.Models.Entities.DonorGenotypePrecomputationRun;
using RunStatus = Atlas.MatchingAlgorithm.Data.Models.Entities.DonorGenotypePrecomputationRunStatus;

namespace Atlas.MatchingAlgorithm.Data.Repositories.Precompute;

/// <summary>
/// Reads and writes the run, batch and group tables of the donor genotype precomputation stage of the data refresh,
/// in one transient database.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every change of batch status is a compare-and-swap:</b> one <c>UPDATE</c> whose <c>WHERE</c> names the statuses the
/// change is allowed from. The workers, the sweeps and the stage write the same rows from different processes, and none
/// of them holds a lock across calls, so the database decides which of two racing writers wins. A method that returns
/// <c>false</c> or an empty list lost such a race, or had nothing to do. Neither is an error.
/// </para>
///
/// <para>
/// <b>Database time, not process time.</b> Every timestamp and every lease check uses <c>SYSUTCDATETIME()</c>, so the
/// workers, the sweeps and the stage agree about when a lease expires, whatever their own clocks say.
/// </para>
/// </remarks>
public interface IDonorGenotypePrecomputationRepository
{
    /// <summary>The run of the data refresh record, or null when the precomputation stage has not created one.</summary>
    Task<DonorGenotypePrecomputationRun?> GetRun(int dataRefreshRecordId);

    /// <summary>
    /// Makes the run of the data refresh record ready for <see cref="BuildRun"/>: a new run with status
    /// <see cref="DonorGenotypePrecomputationRunStatus.Building"/>, or the run of a build that stopped before its end. In the
    /// same transaction it removes the batches of that run and the staging data, so the build starts from nothing.
    /// </summary>
    /// <remarks>
    /// A run that starts its build again keeps its <see cref="DonorGenotypePrecomputationRun.GroupsPerBatch"/>, whatever
    /// <paramref name="groupsPerBatch"/> is now.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The run of the record is not building.</exception>
    Task<DonorGenotypePrecomputationRun> StartBuild(int dataRefreshRecordId, string hlaNomenclatureVersion, int groupsPerBatch);

    /// <summary>
    /// Builds the work of a run from <c>Donors</c>: a group for each distinct typing of each registry and ethnicity pair, at
    /// each <see cref="AllowedLociKey"/>; the donors of each group; and the batches, all
    /// <see cref="DonorGenotypePrecomputationBatchStatus.Pending"/>. Then it moves the run to
    /// <see cref="DonorGenotypePrecomputationRunStatus.Running"/>, or to <see cref="DonorGenotypePrecomputationRunStatus.Completed"/>
    /// when there are no donors: a run with no batches has nothing to wait for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Over all donors at once.</b> Each step is one statement over the whole table, so two donors with the same typing
    /// share a group however far apart they are.
    /// </para>
    ///
    /// <para>
    /// <b>Pair first.</b> The group ids run pair by pair, then key by key, then by typing. So the groups of a batch mostly
    /// have one registry and ethnicity pair, and the worker needs few frequency sets for it.
    /// </para>
    ///
    /// <para>
    /// <b>The run moves only at the end.</b> A build that fails or is cancelled leaves the run building, with part of its
    /// staging data. Call <see cref="StartBuild"/> before the next attempt.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// The run is not building, or it has batches or staging data; or the build lost a donor.
    /// </exception>
    Task<DonorGenotypePrecomputationBuildResult> BuildRun(int runId, CancellationToken cancellationToken);

    /// <summary>
    /// Moves the batch to <see cref="DonorGenotypePrecomputationBatchStatus.InProgress"/> under a new lease, and returns
    /// what the worker needs to process it.
    /// </summary>
    /// <returns>
    /// Null when the batch is not the worker's to take: its run is not
    /// <see cref="DonorGenotypePrecomputationRunStatus.Running"/> or belongs to another refresh, the batch is terminal, or
    /// another worker holds a live lease and the message is not a redelivery. The worker then completes the message and
    /// does nothing.
    /// </returns>
    Task<ClaimedDonorGenotypePrecomputationBatch?> TryClaimBatch(DonorGenotypePrecomputationBatchClaim claim);

    /// <summary>
    /// The groups in the range that have no value yet, in id order, each with the typing and the codes of its
    /// representative donor. A batch that runs again gets only the groups that its earlier attempts did not store.
    /// </summary>
    Task<IReadOnlyList<DonorGenotypePrecomputationGroupToCompute>> GetGroupsToCompute(int runId, int firstGroupId, int lastGroupId);

    /// <summary>
    /// Stores the value id or the failure of each group. A stored value clears an earlier failure of its group. A failure
    /// never replaces a stored value.
    /// </summary>
    /// <exception cref="ArgumentException">More than one outcome for one group.</exception>
    Task RecordGroupOutcomes(int runId, IReadOnlyCollection<DonorGenotypePrecomputationGroupOutcome> outcomes);

    /// <summary>
    /// One donor row for each donor of each group in the range that has a value: what
    /// <see cref="ISubjectGenotypeSetRepository.UpsertDonorAssignments"/> writes for the batch.
    /// </summary>
    Task<IReadOnlyList<DonorSubjectGenotypeSetAssignment>> GetDonorAssignments(int runId, int firstGroupId, int lastGroupId);

    /// <summary>
    /// Moves the batch to <see cref="DonorGenotypePrecomputationBatchStatus.ResultsReceived"/>, and clears its earlier
    /// failure. Only the worker that holds the lease can do this, and only while no one else has taken the batch: from
    /// <see cref="DonorGenotypePrecomputationBatchStatus.InProgress"/>, or from
    /// <see cref="DonorGenotypePrecomputationBatchStatus.Abandoned"/> before a sweep sends the batch again.
    /// </summary>
    /// <returns>False when the worker no longer holds the batch.</returns>
    Task<bool> TryMarkBatchResultsReceived(int batchId, Guid leaseOwner, int failedGroupCount);

    /// <summary>
    /// Moves the batch to <see cref="DonorGenotypePrecomputationBatchStatus.Failed"/>, under the same rule as
    /// <see cref="TryMarkBatchResultsReceived"/>. A sweep then sends it again or gives up on it.
    /// </summary>
    /// <returns>False when the worker no longer holds the batch.</returns>
    Task<bool> TryMarkBatchFailed(int batchId, Guid leaseOwner, DonorGenotypePrecomputationBatchFailure failure);

    /// <summary>
    /// Up to <paramref name="maxCount"/> <see cref="DonorGenotypePrecomputationBatchStatus.Pending"/> batches, in id
    /// order, after <paramref name="afterBatchId"/>. The id bound lets a dispatch go through the batches a chunk at a time
    /// and finish, even when some of them stay pending.
    /// </summary>
    Task<IReadOnlyList<PendingDonorGenotypePrecomputationBatch>> GetPendingBatches(
        int runId,
        PendingBatchSelection selection,
        int afterBatchId,
        int maxCount);

    /// <summary>
    /// Moves the given batches from <see cref="DonorGenotypePrecomputationBatchStatus.Pending"/> to
    /// <see cref="DonorGenotypePrecomputationBatchStatus.Requested"/>, after their messages are published. A batch that a
    /// worker claimed in the meantime stays as it is.
    /// </summary>
    /// <remarks>
    /// <b>Only while the retry count is the one the dispatch read.</b> The messages go out before this update, so a worker
    /// can claim a batch and fail it, and the requeue sweep can send it back to pending, before the update runs. The sweep
    /// gives the batch a new retry count and sends its own message for it. Without the check, this update would move that
    /// batch to requested, the sweep would find no pending batch to send, and the batch would wait for a message that
    /// never comes.
    /// </remarks>
    /// <returns>The number of batches moved.</returns>
    Task<int> MarkBatchesRequested(int runId, IReadOnlyCollection<PendingDonorGenotypePrecomputationBatch> batches);

    /// <summary>
    /// Moves every <see cref="DonorGenotypePrecomputationBatchStatus.InProgress"/> batch whose lease has expired to
    /// <see cref="DonorGenotypePrecomputationBatchStatus.Abandoned"/>. The lease owner stays on the row, so the worker can
    /// still record its result until a sweep sends the batch again.
    /// </summary>
    Task<IReadOnlyList<SweptDonorGenotypePrecomputationBatch>> MarkExpiredBatchesAbandoned(int runId);

    /// <summary>
    /// Moves every failed or abandoned batch with no retries left to
    /// <see cref="DonorGenotypePrecomputationBatchStatus.PermanentlyFailed"/>.
    /// </summary>
    Task<IReadOnlyList<SweptDonorGenotypePrecomputationBatch>> MarkBatchesPermanentlyFailed(int runId, int maxBatchRetries);

    /// <summary>
    /// Moves every failed or abandoned batch that has retries left back to
    /// <see cref="DonorGenotypePrecomputationBatchStatus.Pending"/>. Adds one to its retry count, and clears its lease.
    /// </summary>
    Task<IReadOnlyList<SweptDonorGenotypePrecomputationBatch>> RequeueRetryableBatches(int runId, int maxBatchRetries);

    /// <summary>
    /// Moves a batch whose message was dead-lettered to <see cref="DonorGenotypePrecomputationBatchStatus.Abandoned"/>, so
    /// that the sweeps send it again or give up on it. Only while the run of the given refresh is running, and the batch
    /// is not terminal.
    /// </summary>
    /// <returns>False when the batch was not moved.</returns>
    Task<bool> TryMarkBatchAbandoned(int dataRefreshRecordId, int runId, int batchId, string reason);

    /// <summary>
    /// Moves a <see cref="DonorGenotypePrecomputationRunStatus.Running"/> run whose batches are all terminal to
    /// <see cref="DonorGenotypePrecomputationRunStatus.Completed"/>, or to
    /// <see cref="DonorGenotypePrecomputationRunStatus.CompletedWithFailures"/> when a batch failed permanently or a batch
    /// has failed groups. It only sets the status: the workers have written every result already.
    /// </summary>
    /// <returns>The new status, or null when the run is not running, or a batch is not terminal yet.</returns>
    Task<DonorGenotypePrecomputationRunStatus?> TryFinaliseRun(int runId);

    /// <summary>
    /// Moves the run of a data refresh that has failed to <see cref="DonorGenotypePrecomputationRunStatus.Cancelled"/>, from
    /// <see cref="DonorGenotypePrecomputationRunStatus.Building"/> or <see cref="DonorGenotypePrecomputationRunStatus.Running"/>.
    /// The workers then skip its messages.
    /// </summary>
    /// <returns>False when the record has no run, or its run is done or cancelled already.</returns>
    Task<bool> TryMarkRunCancelled(int dataRefreshRecordId);

    /// <summary>The batches of the run per status, and the failed groups of the batches with results.</summary>
    Task<DonorGenotypePrecomputationBatchCounts> GetBatchCounts(int runId);

    /// <summary>What failed in the run, for the failure threshold and the alerts. Reads the staging tables.</summary>
    /// <returns>Null when the run does not exist.</returns>
    Task<DonorGenotypePrecomputationFailureSummary?> GetFailureSummary(int runId);

    /// <summary>
    /// A manual retry: sends the failed work of a <see cref="DonorGenotypePrecomputationRunStatus.CompletedWithFailures"/>
    /// run back to <see cref="DonorGenotypePrecomputationBatchStatus.Pending"/>, and moves the run back to
    /// <see cref="DonorGenotypePrecomputationRunStatus.Running"/>, in one transaction.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It resets every <see cref="DonorGenotypePrecomputationBatchStatus.PermanentlyFailed"/> batch, and every
    /// <see cref="DonorGenotypePrecomputationBatchStatus.ResultsReceived"/> batch with failed groups, to a retry count of 0
    /// and no failure. A batch that runs again computes only its groups with no value.
    /// </para>
    ///
    /// <para>
    /// <b>To <see cref="DonorGenotypePrecomputationBatchStatus.Pending"/>, not to
    /// <see cref="DonorGenotypePrecomputationBatchStatus.Requested"/>.</b> Requested means that a message is out. The
    /// stage sends the reset batches when it starts again, and the requeue sweep does not send them, because their retry
    /// count is 0.
    /// </para>
    /// </remarks>
    /// <returns>The number of batches reset. 0 when the run is not <see cref="DonorGenotypePrecomputationRunStatus.CompletedWithFailures"/>.</returns>
    Task<int> ResetFailedBatchesForManualRetry(int runId);

    /// <summary>
    /// Removes every row of the group and group-donor staging tables. A database holds one run at a time, so this is the
    /// staging data of that run. The run and batch rows stay.
    /// </summary>
    Task TruncateStagingTables();

    /// <summary>True when the group or the group-donor staging table has a row.</summary>
    Task<bool> HasStagingData();
}

/// <inheritdoc />
public class DonorGenotypePrecomputationRepository : Repository, IDonorGenotypePrecomputationRepository
{
    private const string RunsTableName = "DonorGenotypePrecomputationRuns";
    private const string BatchesTableName = "DonorGenotypePrecomputationBatches";
    private const string GroupsTableName = "DonorGenotypePrecomputationGroups";
    private const string GroupDonorsTableName = "DonorGenotypePrecomputationGroupDonors";
    private const string DonorsTableName = "Donors";
    private const string GroupOutcomeStagingTableName = "#StagedGroupOutcomes";

    private const int CommandTimeoutInSeconds = 300;

    /// <summary>
    /// Long, because the failure summary can read most of the staging data: when most batches of a run failed, it counts
    /// the distinct donors of most of the group-donor rows: four rows for each donor.
    /// </summary>
    private const int FailureSummaryCommandTimeoutInSeconds = 3600;

    /// <summary>The length of <c>FailureMessage</c> on the batch and group tables. A longer message fails the whole update.</summary>
    internal const int FailureMessageMaxLength = 512;

    /// <summary>The failures in a failure summary: enough for an alert to show the causes.</summary>
    internal const int MaxFailureSampleCount = 10;

    /// <summary>Ids per <c>IN</c> list: well below the 2,100 parameters that SQL Server allows in one command.</summary>
    internal const int MaxIdsPerStatement = 1000;

    internal const string LeaseExpiredMessage = "The lease expired before the worker recorded a result.";

    private const string SelectRunSql = $"""
                                         SELECT
                                             {nameof(Run.Id)},
                                             {nameof(Run.DataRefreshRecordId)},
                                             {nameof(Run.HlaNomenclatureVersion)},
                                             {nameof(Run.Status)},
                                             {nameof(Run.GroupsPerBatch)},
                                             {nameof(Run.TotalGroupCount)},
                                             {nameof(Run.TotalBatchCount)},
                                             {nameof(Run.TotalDonorAssignmentCount)},
                                             {nameof(Run.TotalDonorCount)},
                                             {nameof(Run.ManualRetryCount)},
                                             {nameof(Run.CreatedUtc)},
                                             {nameof(Run.StatusDateUtc)},
                                             {nameof(Run.CompletedUtc)}
                                         FROM {RunsTableName}
                                         WHERE {nameof(Run.DataRefreshRecordId)} = @DataRefreshRecordId
                                         """;

    /// <summary>
    /// <para>
    /// <b>The run is checked in the same statement as the claim.</b> A message can outlive its run: the refresh was
    /// cancelled, or it finished while the message waited on the topic. A check of the run before the claim would leave a
    /// gap in which the run could change.
    /// </para>
    ///
    /// <para>
    /// <b>An in-progress batch can be taken in two cases only.</b> Its lease has expired, or the message is a redelivery.
    /// Service Bus delivers a message again only when the worker that had it stopped without completing it, so the earlier
    /// claim has no worker behind it. A second copy of a published message is not a redelivery, and finds the batch held.
    /// </para>
    /// </summary>
    private const string ClaimBatchSql = $"""
                                          UPDATE b
                                          SET
                                              b.{nameof(Batch.Status)} = '{nameof(BatchStatus.InProgress)}',
                                              b.{nameof(Batch.LeaseOwner)} = @LeaseOwner,
                                              b.{nameof(Batch.LeaseExpiresUtc)} = DATEADD(second, @LeaseSeconds, SYSUTCDATETIME()),
                                              b.{nameof(Batch.StatusDateUtc)} = SYSUTCDATETIME()
                                          OUTPUT
                                              inserted.{nameof(Batch.Id)} AS {nameof(ClaimedDonorGenotypePrecomputationBatch.BatchId)},
                                              inserted.{nameof(Batch.RunId)},
                                              inserted.{nameof(Batch.FirstGroupId)},
                                              inserted.{nameof(Batch.LastGroupId)},
                                              inserted.{nameof(Batch.RetryCount)},
                                              r.{nameof(Run.HlaNomenclatureVersion)}
                                          FROM {BatchesTableName} b
                                          INNER JOIN {RunsTableName} r ON r.{nameof(Run.Id)} = b.{nameof(Batch.RunId)}
                                          WHERE b.{nameof(Batch.Id)} = @BatchId
                                            AND b.{nameof(Batch.RunId)} = @RunId
                                            AND r.{nameof(Run.DataRefreshRecordId)} = @DataRefreshRecordId
                                            AND r.{nameof(Run.Status)} = '{nameof(RunStatus.Running)}'
                                            AND (
                                                b.{nameof(Batch.Status)} IN ('{nameof(BatchStatus.Pending)}', '{nameof(BatchStatus.Requested)}')
                                                OR (
                                                    b.{nameof(Batch.Status)} = '{nameof(BatchStatus.InProgress)}'
                                                    AND (@IsRedelivery = 1 OR b.{nameof(Batch.LeaseExpiresUtc)} < SYSUTCDATETIME())))
                                          """;

    /// <summary>
    /// A left join: a representative donor that is missing fails its own group, not the batch. The two DPB1 columns come
    /// too, although no <see cref="AllowedLociKey"/> includes the locus, so that the typing is the whole typing of the donor.
    /// </summary>
    private const string SelectGroupsToComputeSql = $"""
                                                     SELECT
                                                         g.{nameof(Group.Id)} AS {nameof(GroupToComputeRow.GroupId)},
                                                         g.{nameof(Group.AllowedLociKey)},
                                                         g.{nameof(Group.RepresentativeDonorId)},
                                                         d.{nameof(Donor.DonorId)} AS {nameof(GroupToComputeRow.FoundDonorId)},
                                                         d.{nameof(Donor.RegistryCode)},
                                                         d.{nameof(Donor.EthnicityCode)},
                                                         d.{nameof(Donor.A_1)}, d.{nameof(Donor.A_2)},
                                                         d.{nameof(Donor.B_1)}, d.{nameof(Donor.B_2)},
                                                         d.{nameof(Donor.C_1)}, d.{nameof(Donor.C_2)},
                                                         d.{nameof(Donor.DPB1_1)}, d.{nameof(Donor.DPB1_2)},
                                                         d.{nameof(Donor.DQB1_1)}, d.{nameof(Donor.DQB1_2)},
                                                         d.{nameof(Donor.DRB1_1)}, d.{nameof(Donor.DRB1_2)}
                                                     FROM {GroupsTableName} g
                                                     LEFT JOIN {DonorsTableName} d ON d.{nameof(Donor.DonorId)} = g.{nameof(Group.RepresentativeDonorId)}
                                                     WHERE g.{nameof(Group.RunId)} = @RunId
                                                       AND g.{nameof(Group.Id)} BETWEEN @FirstGroupId AND @LastGroupId
                                                       AND g.{nameof(Group.SubjectGenotypeSetValueId)} IS NULL
                                                     ORDER BY g.{nameof(Group.Id)}
                                                     """;

    /// <summary>
    /// <c>COLLATE DATABASE_DEFAULT</c>: a temp table otherwise takes the collation of tempdb, and the update would then
    /// write a message in a collation that its column does not have.
    /// </summary>
    private const string CreateGroupOutcomeStagingTableSql = $"""
                                                              CREATE TABLE {GroupOutcomeStagingTableName} (
                                                                  {nameof(Outcome.GroupId)}                   int           NOT NULL PRIMARY KEY,
                                                                  {nameof(Outcome.SubjectGenotypeSetValueId)} int           NULL,
                                                                  {nameof(Outcome.FailureMessage)}            nvarchar(512) COLLATE DATABASE_DEFAULT NULL)
                                                              """;

    /// <summary>
    /// A stored value replaces the failure of its group, and a failure never replaces a stored value: the
    /// <c>COALESCE</c> keeps a value that a concurrent attempt at the same batch stored first.
    /// </summary>
    private const string UpdateGroupsFromStagedOutcomesSql = $"""
                                                              UPDATE g
                                                              SET
                                                                  g.{nameof(Group.SubjectGenotypeSetValueId)} =
                                                                      COALESCE(o.{nameof(Outcome.SubjectGenotypeSetValueId)}, g.{nameof(Group.SubjectGenotypeSetValueId)}),
                                                                  g.{nameof(Group.FailureMessage)} = CASE
                                                                      WHEN COALESCE(o.{nameof(Outcome.SubjectGenotypeSetValueId)}, g.{nameof(Group.SubjectGenotypeSetValueId)}) IS NULL
                                                                          THEN o.{nameof(Outcome.FailureMessage)}
                                                                      ELSE NULL
                                                                  END
                                                              FROM {GroupsTableName} g
                                                              INNER JOIN {GroupOutcomeStagingTableName} o ON o.{nameof(Outcome.GroupId)} = g.{nameof(Group.Id)}
                                                              WHERE g.{nameof(Group.RunId)} = @RunId
                                                              """;

    private const string SelectDonorAssignmentsSql = $"""
                                                      SELECT
                                                          gd.{nameof(GroupDonor.DonorId)},
                                                          g.{nameof(Group.AllowedLociKey)},
                                                          g.{nameof(Group.SubjectGenotypeSetValueId)}
                                                      FROM {GroupDonorsTableName} gd
                                                      INNER JOIN {GroupsTableName} g ON g.{nameof(Group.Id)} = gd.{nameof(GroupDonor.GroupId)}
                                                      WHERE gd.{nameof(GroupDonor.GroupId)} BETWEEN @FirstGroupId AND @LastGroupId
                                                        AND g.{nameof(Group.RunId)} = @RunId
                                                        AND g.{nameof(Group.SubjectGenotypeSetValueId)} IS NOT NULL
                                                      """;

    /// <summary>
    /// The statuses from which the lease owner can finish a batch. <see cref="BatchStatus.Abandoned"/> is one of them: a
    /// sweep marks an expired lease abandoned while its worker can still be alive, and about to finish. The requeue
    /// clears the lease, and from then on only the next claim can finish the batch.
    /// </summary>
    private const string OwnerCanFinishCondition = $"""
                                                    {nameof(Batch.Id)} = @BatchId
                                                      AND {nameof(Batch.LeaseOwner)} = @LeaseOwner
                                                      AND {nameof(Batch.Status)} IN ('{nameof(BatchStatus.InProgress)}', '{nameof(BatchStatus.Abandoned)}')
                                                    """;

    private const string MarkBatchResultsReceivedSql = $"""
                                                        UPDATE {BatchesTableName}
                                                        SET
                                                            {nameof(Batch.Status)} = '{nameof(BatchStatus.ResultsReceived)}',
                                                            {nameof(Batch.FailedGroupCount)} = @FailedGroupCount,
                                                            {nameof(Batch.FailureMessage)} = NULL,
                                                            {nameof(Batch.FailureException)} = NULL,
                                                            {nameof(Batch.StatusDateUtc)} = SYSUTCDATETIME(),
                                                            {nameof(Batch.CompletedUtc)} = SYSUTCDATETIME()
                                                        WHERE {OwnerCanFinishCondition}
                                                        """;

    private const string MarkBatchFailedSql = $"""
                                               UPDATE {BatchesTableName}
                                               SET
                                                   {nameof(Batch.Status)} = '{nameof(BatchStatus.Failed)}',
                                                   {nameof(Batch.FailureMessage)} = @FailureMessage,
                                                   {nameof(Batch.FailureException)} = @FailureException,
                                                   {nameof(Batch.FailedGroupCount)} = @FailedGroupCount,
                                                   {nameof(Batch.StatusDateUtc)} = SYSUTCDATETIME()
                                               WHERE {OwnerCanFinishCondition}
                                               """;

    private const string SelectPendingBatchesSql = $"""
                                                     SELECT TOP (@MaxCount)
                                                         {nameof(Batch.Id)} AS {nameof(PendingDonorGenotypePrecomputationBatch.BatchId)},
                                                         {nameof(Batch.RetryCount)}
                                                     FROM {BatchesTableName}
                                                     WHERE {nameof(Batch.RunId)} = @RunId
                                                       AND {nameof(Batch.Status)} = '{nameof(BatchStatus.Pending)}'
                                                       AND {nameof(Batch.Id)} > @AfterBatchId
                                                       AND (@RequeuedOnly = 0 OR {nameof(Batch.RetryCount)} > 0)
                                                     ORDER BY {nameof(Batch.Id)}
                                                     """;

    private const string MarkBatchesRequestedSql = $"""
                                                    UPDATE {BatchesTableName}
                                                    SET
                                                        {nameof(Batch.Status)} = '{nameof(BatchStatus.Requested)}',
                                                        {nameof(Batch.DispatchedUtc)} = SYSUTCDATETIME(),
                                                        {nameof(Batch.StatusDateUtc)} = SYSUTCDATETIME()
                                                    WHERE {nameof(Batch.RunId)} = @RunId
                                                      AND {nameof(Batch.Status)} = '{nameof(BatchStatus.Pending)}'
                                                      AND {nameof(Batch.RetryCount)} = @RetryCount
                                                      AND {nameof(Batch.Id)} IN @BatchIds
                                                    """;

    /// <summary>What every sweep returns: each batch it moved, as the batch was just before the move.</summary>
    private const string SweptBatchOutput = $"""
                                             OUTPUT
                                                 inserted.{nameof(Batch.Id)} AS {nameof(SweptBatchRow.BatchId)},
                                                 deleted.{nameof(Batch.Status)} AS {nameof(SweptBatchRow.PreviousStatus)},
                                                 deleted.{nameof(Batch.RetryCount)},
                                                 deleted.{nameof(Batch.FailureMessage)}
                                             """;

    private const string MarkExpiredBatchesAbandonedSql = $"""
                                                           UPDATE {BatchesTableName}
                                                           SET
                                                               {nameof(Batch.Status)} = '{nameof(BatchStatus.Abandoned)}',
                                                               {nameof(Batch.FailureMessage)} = @LeaseExpiredMessage,
                                                               {nameof(Batch.StatusDateUtc)} = SYSUTCDATETIME()
                                                           {SweptBatchOutput}
                                                           WHERE {nameof(Batch.RunId)} = @RunId
                                                             AND {nameof(Batch.Status)} = '{nameof(BatchStatus.InProgress)}'
                                                             AND {nameof(Batch.LeaseExpiresUtc)} < SYSUTCDATETIME()
                                                           """;

    private const string MarkBatchesPermanentlyFailedSql = $"""
                                                            UPDATE {BatchesTableName}
                                                            SET
                                                                {nameof(Batch.Status)} = '{nameof(BatchStatus.PermanentlyFailed)}',
                                                                {nameof(Batch.StatusDateUtc)} = SYSUTCDATETIME(),
                                                                {nameof(Batch.CompletedUtc)} = SYSUTCDATETIME()
                                                            {SweptBatchOutput}
                                                            WHERE {nameof(Batch.RunId)} = @RunId
                                                              AND {nameof(Batch.Status)} IN ('{nameof(BatchStatus.Failed)}', '{nameof(BatchStatus.Abandoned)}')
                                                              AND {nameof(Batch.RetryCount)} >= @MaxBatchRetries
                                                            """;

    /// <summary>
    /// The exact complement of <see cref="MarkBatchesPermanentlyFailedSql"/> over failed and abandoned batches, so the two
    /// sweeps together leave neither status behind: a batch that no sweep can reach would never finish.
    /// </summary>
    private const string RequeueRetryableBatchesSql = $"""
                                                       UPDATE {BatchesTableName}
                                                       SET
                                                           {nameof(Batch.Status)} = '{nameof(BatchStatus.Pending)}',
                                                           {nameof(Batch.RetryCount)} = {nameof(Batch.RetryCount)} + 1,
                                                           {nameof(Batch.LeaseOwner)} = NULL,
                                                           {nameof(Batch.LeaseExpiresUtc)} = NULL,
                                                           {nameof(Batch.StatusDateUtc)} = SYSUTCDATETIME()
                                                       {SweptBatchOutput}
                                                       WHERE {nameof(Batch.RunId)} = @RunId
                                                         AND {nameof(Batch.Status)} IN ('{nameof(BatchStatus.Failed)}', '{nameof(BatchStatus.Abandoned)}')
                                                         AND {nameof(Batch.RetryCount)} < @MaxBatchRetries
                                                       """;

    private const string MarkBatchAbandonedSql = $"""
                                                  UPDATE b
                                                  SET
                                                      b.{nameof(Batch.Status)} = '{nameof(BatchStatus.Abandoned)}',
                                                      b.{nameof(Batch.FailureMessage)} = @Reason,
                                                      b.{nameof(Batch.StatusDateUtc)} = SYSUTCDATETIME()
                                                  FROM {BatchesTableName} b
                                                  INNER JOIN {RunsTableName} r ON r.{nameof(Run.Id)} = b.{nameof(Batch.RunId)}
                                                  WHERE b.{nameof(Batch.Id)} = @BatchId
                                                    AND b.{nameof(Batch.RunId)} = @RunId
                                                    AND r.{nameof(Run.DataRefreshRecordId)} = @DataRefreshRecordId
                                                    AND r.{nameof(Run.Status)} = '{nameof(RunStatus.Running)}'
                                                    AND b.{nameof(Batch.Status)} IN ('{nameof(BatchStatus.Pending)}', '{nameof(BatchStatus.Requested)}', '{nameof(BatchStatus.InProgress)}')
                                                  """;

    /// <summary>
    /// <para>
    /// <b>Safe with no lock across calls.</b> A terminal batch does not move while its run is running: only the manual
    /// reset moves it, and the reset needs a completed run. So when the check finds no batch left to finish, no batch can
    /// start again before the update.
    /// </para>
    ///
    /// <para>
    /// A batch with failed groups makes the run <see cref="RunStatus.CompletedWithFailures"/>: the donors of those groups
    /// have no rows.
    /// </para>
    /// </summary>
    private const string FinaliseRunSql = $"""
                                           UPDATE r
                                           SET
                                               r.{nameof(Run.Status)} = CASE
                                                   WHEN EXISTS (
                                                       SELECT 1 FROM {BatchesTableName} b
                                                       WHERE b.{nameof(Batch.RunId)} = r.{nameof(Run.Id)}
                                                         AND (b.{nameof(Batch.Status)} = '{nameof(BatchStatus.PermanentlyFailed)}'
                                                              OR b.{nameof(Batch.FailedGroupCount)} > 0))
                                                       THEN '{nameof(RunStatus.CompletedWithFailures)}'
                                                   ELSE '{nameof(RunStatus.Completed)}'
                                               END,
                                               r.{nameof(Run.StatusDateUtc)} = SYSUTCDATETIME(),
                                               r.{nameof(Run.CompletedUtc)} = SYSUTCDATETIME()
                                           OUTPUT inserted.{nameof(Run.Status)}
                                           FROM {RunsTableName} r
                                           WHERE r.{nameof(Run.Id)} = @RunId
                                             AND r.{nameof(Run.Status)} = '{nameof(RunStatus.Running)}'
                                             AND NOT EXISTS (
                                                 SELECT 1 FROM {BatchesTableName} b
                                                 WHERE b.{nameof(Batch.RunId)} = r.{nameof(Run.Id)}
                                                   AND b.{nameof(Batch.Status)} NOT IN (
                                                       '{nameof(BatchStatus.ResultsReceived)}', '{nameof(BatchStatus.PermanentlyFailed)}'))
                                           """;

    private const string SelectBatchCountsSql = $"""
                                                 SELECT
                                                     {nameof(Batch.Status)},
                                                     COUNT(*) AS {nameof(BatchCountRow.BatchCount)},
                                                     SUM(CASE
                                                         WHEN {nameof(Batch.Status)} = '{nameof(BatchStatus.ResultsReceived)}' THEN {nameof(Batch.FailedGroupCount)}
                                                         ELSE 0
                                                     END) AS {nameof(BatchCountRow.FailedGroupCount)}
                                                 FROM {BatchesTableName}
                                                 WHERE {nameof(Batch.RunId)} = @RunId
                                                 GROUP BY {nameof(Batch.Status)}
                                                 """;

    private const string SelectRunDonorCountSql = $"""
                                                   SELECT {nameof(Run.Id)}, {nameof(Run.TotalDonorCount)}
                                                   FROM {RunsTableName}
                                                   WHERE {nameof(Run.Id)} = @RunId
                                                   """;

    /// <summary>
    /// The groups with no value in a batch with results. Only the batches with failed groups are read, and each of them
    /// costs one range seek on the groups, so a run with few failures reads few groups.
    /// </summary>
    private const string FailedGroupsOfBatchesWithResultsSource = $"""
                                                                   FROM {BatchesTableName} b
                                                                   INNER JOIN {GroupsTableName} g
                                                                       ON g.{nameof(Group.Id)} BETWEEN b.{nameof(Batch.FirstGroupId)} AND b.{nameof(Batch.LastGroupId)}
                                                                   WHERE b.{nameof(Batch.RunId)} = @RunId
                                                                     AND b.{nameof(Batch.Status)} = '{nameof(BatchStatus.ResultsReceived)}'
                                                                     AND b.{nameof(Batch.FailedGroupCount)} > 0
                                                                     AND g.{nameof(Group.RunId)} = @RunId
                                                                     AND g.{nameof(Group.SubjectGenotypeSetValueId)} IS NULL
                                                                   """;

    /// <summary>
    /// The donors are counted through the groups of the failed batches, not through their values or donor rows: a batch
    /// that stopped part way can have stored values and written no donor rows.
    /// </summary>
    private const string SelectFailureCountsSql = $"""
                                                   WITH FailedGroups AS (
                                                       SELECT g.{nameof(Group.Id)} AS GroupId, b.{nameof(Batch.Status)} AS BatchStatus
                                                       FROM {BatchesTableName} b
                                                       INNER JOIN {GroupsTableName} g
                                                           ON g.{nameof(Group.Id)} BETWEEN b.{nameof(Batch.FirstGroupId)} AND b.{nameof(Batch.LastGroupId)}
                                                       WHERE b.{nameof(Batch.RunId)} = @RunId
                                                         AND b.{nameof(Batch.Status)} = '{nameof(BatchStatus.PermanentlyFailed)}'
                                                         AND g.{nameof(Group.RunId)} = @RunId
                                                       UNION ALL
                                                       SELECT g.{nameof(Group.Id)}, b.{nameof(Batch.Status)}
                                                       {FailedGroupsOfBatchesWithResultsSource}
                                                   )
                                                   SELECT
                                                       (SELECT COUNT(*)
                                                        FROM {BatchesTableName}
                                                        WHERE {nameof(Batch.RunId)} = @RunId
                                                          AND {nameof(Batch.Status)} = '{nameof(BatchStatus.PermanentlyFailed)}')
                                                           AS {nameof(FailureCountsRow.PermanentlyFailedBatchCount)},
                                                       (SELECT COUNT(*)
                                                        FROM FailedGroups
                                                        WHERE BatchStatus = '{nameof(BatchStatus.ResultsReceived)}')
                                                           AS {nameof(FailureCountsRow.FailedGroupCount)},
                                                       (SELECT COUNT(DISTINCT gd.{nameof(GroupDonor.DonorId)})
                                                        FROM FailedGroups fg
                                                        INNER JOIN {GroupDonorsTableName} gd ON gd.{nameof(GroupDonor.GroupId)} = fg.GroupId)
                                                           AS {nameof(FailureCountsRow.FailedDonorCount)}
                                                   """;

    /// <summary>
    /// The first failure of each distinct message: one cause that failed many batches, such as a database outage, then
    /// takes one sample, and the other samples show other causes.
    /// </summary>
    private const string SelectFailureSamplesSql = $"""
                                                    SELECT TOP (@MaxSampleCount)
                                                        numbered.{nameof(FailureSampleRow.BatchId)},
                                                        numbered.{nameof(FailureSampleRow.GroupId)},
                                                        numbered.{nameof(FailureSampleRow.FailureMessage)}
                                                    FROM (
                                                        SELECT
                                                            failures.*,
                                                            ROW_NUMBER() OVER (
                                                                PARTITION BY failures.{nameof(FailureSampleRow.FailureMessage)}
                                                                ORDER BY failures.IsGroupFailure, failures.{nameof(FailureSampleRow.BatchId)}, failures.{nameof(FailureSampleRow.GroupId)}
                                                            ) AS Occurrence
                                                        FROM (
                                                            SELECT
                                                                b.{nameof(Batch.Id)} AS {nameof(FailureSampleRow.BatchId)},
                                                                CAST(NULL AS int) AS {nameof(FailureSampleRow.GroupId)},
                                                                b.{nameof(Batch.FailureMessage)} AS {nameof(FailureSampleRow.FailureMessage)},
                                                                0 AS IsGroupFailure
                                                            FROM {BatchesTableName} b
                                                            WHERE b.{nameof(Batch.RunId)} = @RunId
                                                              AND b.{nameof(Batch.Status)} = '{nameof(BatchStatus.PermanentlyFailed)}'
                                                            UNION ALL
                                                            SELECT b.{nameof(Batch.Id)}, g.{nameof(Group.Id)}, g.{nameof(Group.FailureMessage)}, 1
                                                            {FailedGroupsOfBatchesWithResultsSource}
                                                        ) failures
                                                    ) numbered
                                                    WHERE numbered.Occurrence = 1
                                                    ORDER BY numbered.IsGroupFailure, numbered.{nameof(FailureSampleRow.BatchId)}, numbered.{nameof(FailureSampleRow.GroupId)}
                                                    """;

    private const string ResetRunForManualRetrySql = $"""
                                                      UPDATE {RunsTableName}
                                                      SET
                                                          {nameof(Run.Status)} = '{nameof(RunStatus.Running)}',
                                                          {nameof(Run.ManualRetryCount)} = {nameof(Run.ManualRetryCount)} + 1,
                                                          {nameof(Run.StatusDateUtc)} = SYSUTCDATETIME(),
                                                          {nameof(Run.CompletedUtc)} = NULL
                                                      WHERE {nameof(Run.Id)} = @RunId
                                                        AND {nameof(Run.Status)} = '{nameof(RunStatus.CompletedWithFailures)}'
                                                      """;

    /// <summary>
    /// The same batches that made the run <see cref="RunStatus.CompletedWithFailures"/> (see <see cref="FinaliseRunSql"/>).
    /// </summary>
    private const string ResetFailedBatchesSql = $"""
                                                  UPDATE {BatchesTableName}
                                                  SET
                                                      {nameof(Batch.Status)} = '{nameof(BatchStatus.Pending)}',
                                                      {nameof(Batch.RetryCount)} = 0,
                                                      {nameof(Batch.FailureMessage)} = NULL,
                                                      {nameof(Batch.FailureException)} = NULL,
                                                      {nameof(Batch.FailedGroupCount)} = 0,
                                                      {nameof(Batch.LeaseOwner)} = NULL,
                                                      {nameof(Batch.LeaseExpiresUtc)} = NULL,
                                                      {nameof(Batch.StatusDateUtc)} = SYSUTCDATETIME(),
                                                      {nameof(Batch.CompletedUtc)} = NULL
                                                  WHERE {nameof(Batch.RunId)} = @RunId
                                                    AND (
                                                        {nameof(Batch.Status)} = '{nameof(BatchStatus.PermanentlyFailed)}'
                                                        OR ({nameof(Batch.Status)} = '{nameof(BatchStatus.ResultsReceived)}' AND {nameof(Batch.FailedGroupCount)} > 0))
                                                  """;

    /// <summary>
    /// <c>TRUNCATE</c> is not allowed on a table that a foreign key points at. No foreign key points at either table: keep
    /// it so.
    /// </summary>
    private const string TruncateStagingTablesSql = $"""
                                                     TRUNCATE TABLE {GroupDonorsTableName};
                                                     TRUNCATE TABLE {GroupsTableName};
                                                     """;

    private const string StagingDataExistsCondition =
        $"EXISTS (SELECT 1 FROM {GroupsTableName}) OR EXISTS (SELECT 1 FROM {GroupDonorsTableName})";

    private const string HasStagingDataSql = $"SELECT CASE WHEN {StagingDataExistsCondition} THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END";

    private const string MarkRunCancelledSql = $"""
                                                UPDATE {RunsTableName}
                                                SET
                                                    {nameof(Run.Status)} = '{nameof(RunStatus.Cancelled)}',
                                                    {nameof(Run.StatusDateUtc)} = SYSUTCDATETIME()
                                                WHERE {nameof(Run.DataRefreshRecordId)} = @DataRefreshRecordId
                                                  AND {nameof(Run.Status)} IN ('{nameof(RunStatus.Building)}', '{nameof(RunStatus.Running)}')
                                                """;

    // The start and the build of a run.

    /// <summary>
    /// The locks last to the end of the transaction, so two calls for one record cannot both find no run and insert one.
    /// </summary>
    private const string SelectRunToStartBuildSql = $"""
                                                     SELECT {nameof(Run.Id)}, {nameof(Run.Status)}
                                                     FROM {RunsTableName} WITH (UPDLOCK, HOLDLOCK)
                                                     WHERE {nameof(Run.DataRefreshRecordId)} = @DataRefreshRecordId
                                                     """;

    private const string InsertBuildingRunSql = $"""
                                                 INSERT INTO {RunsTableName} (
                                                     {nameof(Run.DataRefreshRecordId)},
                                                     {nameof(Run.HlaNomenclatureVersion)},
                                                     {nameof(Run.Status)},
                                                     {nameof(Run.GroupsPerBatch)},
                                                     {nameof(Run.ManualRetryCount)},
                                                     {nameof(Run.CreatedUtc)},
                                                     {nameof(Run.StatusDateUtc)})
                                                 VALUES (
                                                     @DataRefreshRecordId,
                                                     @HlaNomenclatureVersion,
                                                     '{nameof(RunStatus.Building)}',
                                                     @GroupsPerBatch,
                                                     0,
                                                     SYSUTCDATETIME(),
                                                     SYSUTCDATETIME())
                                                 """;

    private const string RestartBuildSql = $"""
                                            DELETE FROM {BatchesTableName} WHERE {nameof(Batch.RunId)} = @RunId;
                                            UPDATE {RunsTableName} SET {nameof(Run.StatusDateUtc)} = SYSUTCDATETIME() WHERE {nameof(Run.Id)} = @RunId;
                                            """;

    private const string SelectRunToBuildSql = $"""
                                                SELECT
                                                    r.{nameof(Run.Status)},
                                                    r.{nameof(Run.GroupsPerBatch)},
                                                    CASE WHEN EXISTS (SELECT 1 FROM {BatchesTableName} b WHERE b.{nameof(Batch.RunId)} = r.{nameof(Run.Id)})
                                                        THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS {nameof(RunToBuildRow.HasBatches)},
                                                    CASE WHEN {StagingDataExistsCondition}
                                                        THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS {nameof(RunToBuildRow.HasStagingData)}
                                                FROM {RunsTableName} r
                                                WHERE r.{nameof(Run.Id)} = @RunId
                                                """;

    /// <summary>
    /// The longest steps of the build read or write a row for each donor at each key, so on a full donor set they take a
    /// long time. Twelve hours, as for the creation of the HLA indexes.
    /// </summary>
    private const int BuildCommandTimeoutInSeconds = 43200;

    private const string BuildPairsTableName = "#BuildPairs";
    private const string BuildDonorTypingsTableName = "#BuildDonorTypings";
    private const string BuildGroupRangesTableName = "#BuildGroupRanges";

    /// <summary>
    /// The keys of the build, in the order of the group ids within a pair. The ordinal of a key (from 1) names its typing
    /// hash column and its range of group ids.
    /// </summary>
    private static readonly IReadOnlyList<AllowedLociKey> BuildKeys = AllowedLociKeyExtensions.All;

    private static readonly IReadOnlyList<int> BuildKeyOrdinals = [.. Enumerable.Range(1, BuildKeys.Count)];

    /// <summary>
    /// The two position columns of each locus in <c>Donors</c>, in the locus order of <c>SubjectGenotypeSetKeyGenerator</c>.
    /// DPB1 is not here: no <see cref="AllowedLociKey"/> includes it.
    /// </summary>
    private static readonly (Locus Locus, string FirstPositionColumn, string SecondPositionColumn)[] TypingColumns =
    [
        (Locus.A, nameof(Donor.A_1), nameof(Donor.A_2)),
        (Locus.B, nameof(Donor.B_1), nameof(Donor.B_2)),
        (Locus.C, nameof(Donor.C_1), nameof(Donor.C_2)),
        (Locus.Drb1, nameof(Donor.DRB1_1), nameof(Donor.DRB1_2)),
        (Locus.Dqb1, nameof(Donor.DQB1_1), nameof(Donor.DQB1_2))
    ];

    /// <summary>
    /// The registry and ethnicity pair of the donor <c>d</c> as one value: the hash of each code, side by side.
    /// <list type="bullet">
    /// <item>A hash reads the exact bytes. <c>=</c> and <c>GROUP BY</c> ignore trailing spaces, also under a binary
    /// collation, and the default collation ignores case. The frequency set lookup in C# does neither, so two codes that
    /// SQL Server compares as equal can get different frequency sets.</item>
    /// <item>Each hash has a fixed length, so two different pairs cannot make the same value.</item>
    /// <item>The prefix keeps a missing code apart from an empty one.</item>
    /// </list>
    /// </summary>
    private const string DonorPairKeySql = $"""
                                            HASHBYTES('SHA2_256', ISNULL(N'1' + d.{nameof(Donor.RegistryCode)}, N'0'))
                                                + HASHBYTES('SHA2_256', ISNULL(N'1' + d.{nameof(Donor.EthnicityCode)}, N'0'))
                                            """;

    private static readonly string CreateBuildTablesSql = $"""
                                                          CREATE TABLE {BuildPairsTableName} (
                                                              PairKey binary(64) NOT NULL PRIMARY KEY,
                                                              PairId  int        NOT NULL);

                                                          CREATE TABLE {BuildDonorTypingsTableName} (
                                                              DonorId int NOT NULL,
                                                              PairId  int NOT NULL,
                                                              {string.Join(", ", BuildKeyOrdinals.Select(ordinal => $"{TypingHashColumn(ordinal)} binary(32) NOT NULL"))});

                                                          CREATE TABLE {BuildGroupRangesTableName} (
                                                              PairId       int     NOT NULL,
                                                              KeyOrdinal   tinyint NOT NULL,
                                                              GroupCount   int     NOT NULL,
                                                              FirstGroupId int     NULL,
                                                              PRIMARY KEY (PairId, KeyOrdinal));
                                                          """;

    /// <summary>
    /// The pairs, numbered in the order of the group ids: by the codes, compared by code point, each followed by its byte
    /// length to keep apart two codes that differ only in trailing spaces. The codes of one pair key are equal byte for
    /// byte, so <c>MAX</c> gives them.
    /// </summary>
    private const string InsertPairsSql = $"""
                                           INSERT INTO {BuildPairsTableName} (PairKey, PairId)
                                           SELECT
                                               pairs.PairKey,
                                               ROW_NUMBER() OVER (ORDER BY
                                                   pairs.RegistryCode COLLATE Latin1_General_BIN2, DATALENGTH(pairs.RegistryCode),
                                                   pairs.EthnicityCode COLLATE Latin1_General_BIN2, DATALENGTH(pairs.EthnicityCode))
                                           FROM (
                                               SELECT keyed.PairKey, MAX(keyed.RegistryCode) AS RegistryCode, MAX(keyed.EthnicityCode) AS EthnicityCode
                                               FROM (
                                                   SELECT
                                                       {DonorPairKeySql} AS PairKey,
                                                       d.{nameof(Donor.RegistryCode)} AS RegistryCode,
                                                       d.{nameof(Donor.EthnicityCode)} AS EthnicityCode
                                                   FROM {DonorsTableName} d
                                               ) keyed
                                               GROUP BY keyed.PairKey
                                           ) pairs
                                           """;

    /// <summary>Each donor, with its pair and the hash of its typing at each key.</summary>
    private static readonly string InsertDonorTypingsSql = $"""
                                                           INSERT INTO {BuildDonorTypingsTableName} WITH (TABLOCK) (
                                                               DonorId,
                                                               PairId,
                                                               {string.Join(", ", BuildKeyOrdinals.Select(TypingHashColumn))})
                                                           SELECT
                                                               d.{nameof(Donor.DonorId)},
                                                               p.PairId,
                                                               {string.Join(", ", BuildKeys.Select(TypingHashSql))}
                                                           FROM {DonorsTableName} d
                                                           INNER JOIN {BuildPairsTableName} p ON p.PairKey = {DonorPairKeySql}
                                                           """;

    /// <summary>
    /// The first group id of each pair and key: a running total of the group counts, pair by pair, then key by key. So the
    /// group ids of the run start at 1 and have no gap.
    /// </summary>
    private const string SetFirstGroupIdsSql = $"""
                                                UPDATE r
                                                SET r.FirstGroupId = numbered.FirstGroupId
                                                FROM {BuildGroupRangesTableName} r
                                                INNER JOIN (
                                                    SELECT
                                                        PairId,
                                                        KeyOrdinal,
                                                        SUM(GroupCount) OVER (ORDER BY PairId, KeyOrdinal ROWS UNBOUNDED PRECEDING) - GroupCount + 1 AS FirstGroupId
                                                    FROM {BuildGroupRangesTableName}
                                                ) numbered ON numbered.PairId = r.PairId AND numbered.KeyOrdinal = r.KeyOrdinal
                                                """;

    /// <summary>
    /// The groups, from their donors. The lowest donor id of a group is its representative: the worker reads the typing and
    /// the codes of that donor.
    /// </summary>
    private static readonly string InsertGroupsSql = $"""
                                                     INSERT INTO {GroupsTableName} WITH (TABLOCK) (
                                                         {nameof(Group.Id)},
                                                         {nameof(Group.RunId)},
                                                         {nameof(Group.AllowedLociKey)},
                                                         {nameof(Group.RepresentativeDonorId)},
                                                         {nameof(Group.DonorCount)})
                                                     SELECT
                                                         gd.{nameof(GroupDonor.GroupId)},
                                                         @RunId,
                                                         lociKeys.AllowedLociKey,
                                                         MIN(gd.{nameof(GroupDonor.DonorId)}),
                                                         COUNT(*)
                                                     FROM {BuildGroupRangesTableName} r
                                                     INNER JOIN (VALUES {string.Join(", ", BuildKeyOrdinals.Select(ordinal => $"({ordinal}, N'{BuildKeys[ordinal - 1]}')"))}) lociKeys (KeyOrdinal, AllowedLociKey)
                                                         ON lociKeys.KeyOrdinal = r.KeyOrdinal
                                                     INNER JOIN {GroupDonorsTableName} gd
                                                         ON gd.{nameof(GroupDonor.GroupId)} BETWEEN r.FirstGroupId AND r.FirstGroupId + r.GroupCount - 1
                                                     GROUP BY gd.{nameof(GroupDonor.GroupId)}, lociKeys.AllowedLociKey
                                                     """;

    /// <summary>The batches: up to <c>GroupsPerBatch</c> groups each, in id order, so the groups of a batch are one range.</summary>
    private const string InsertBatchesSql = $"""
                                             INSERT INTO {BatchesTableName} (
                                                 {nameof(Batch.RunId)},
                                                 {nameof(Batch.BatchNumber)},
                                                 {nameof(Batch.FirstGroupId)},
                                                 {nameof(Batch.LastGroupId)},
                                                 {nameof(Batch.GroupCount)},
                                                 {nameof(Batch.DonorAssignmentCount)},
                                                 {nameof(Batch.Status)},
                                                 {nameof(Batch.RetryCount)},
                                                 {nameof(Batch.FailedGroupCount)},
                                                 {nameof(Batch.StatusDateUtc)})
                                             SELECT
                                                 @RunId,
                                                 numbered.BatchNumber,
                                                 MIN(numbered.GroupId),
                                                 MAX(numbered.GroupId),
                                                 COUNT(*),
                                                 SUM(numbered.DonorCount),
                                                 '{nameof(BatchStatus.Pending)}',
                                                 0,
                                                 0,
                                                 SYSUTCDATETIME()
                                             FROM (
                                                 SELECT
                                                     (g.{nameof(Group.Id)} - 1) / @GroupsPerBatch AS BatchNumber,
                                                     g.{nameof(Group.Id)} AS GroupId,
                                                     g.{nameof(Group.DonorCount)} AS DonorCount
                                                 FROM {GroupsTableName} g
                                                 WHERE g.{nameof(Group.RunId)} = @RunId
                                             ) numbered
                                             GROUP BY numbered.BatchNumber
                                             """;

    /// <summary>
    /// The end of the build: a compare-and-swap from building. A run with no batches is complete at once: no worker and no
    /// sweep has anything to do for it.
    /// </summary>
    private const string FinishBuildSql = $"""
                                           UPDATE {RunsTableName}
                                           SET
                                               {nameof(Run.Status)} = CASE
                                                   WHEN @TotalBatchCount = 0 THEN '{nameof(RunStatus.Completed)}'
                                                   ELSE '{nameof(RunStatus.Running)}'
                                               END,
                                               {nameof(Run.TotalGroupCount)} = @TotalGroupCount,
                                               {nameof(Run.TotalBatchCount)} = @TotalBatchCount,
                                               {nameof(Run.TotalDonorAssignmentCount)} = @TotalDonorAssignmentCount,
                                               {nameof(Run.TotalDonorCount)} = @TotalDonorCount,
                                               {nameof(Run.StatusDateUtc)} = SYSUTCDATETIME(),
                                               {nameof(Run.CompletedUtc)} = CASE WHEN @TotalBatchCount = 0 THEN SYSUTCDATETIME() END
                                           WHERE {nameof(Run.Id)} = @RunId
                                             AND {nameof(Run.Status)} = '{nameof(RunStatus.Building)}'
                                           """;

    private const string CountDonorsSql = $"SELECT COUNT(*) FROM {DonorsTableName}";

    private const string CountRangeGroupsSql = $"SELECT ISNULL(SUM(GroupCount), 0) FROM {BuildGroupRangesTableName}";

    private const string DropBuildTablesSql = $"""
                                               DROP TABLE IF EXISTS {BuildGroupRangesTableName};
                                               DROP TABLE IF EXISTS {BuildDonorTypingsTableName};
                                               DROP TABLE IF EXISTS {BuildPairsTableName};
                                               """;

    private static string TypingHashColumn(int keyOrdinal) => $"TypingHash{keyOrdinal}";

    /// <summary>
    /// The hash of the typing of the donor <c>d</c> at the loci of <paramref name="allowedLociKey"/>: each position followed by
    /// U+001F, the canonical string of <c>SubjectGenotypeSetKeyGenerator</c>. So two donors have one hash exactly when they
    /// have one stored typing key. <c>CONCAT</c> reads a missing position as an empty one, as that key does.
    /// </summary>
    private static string TypingHashSql(AllowedLociKey allowedLociKey)
    {
        var loci = allowedLociKey.ToLoci();
        var positions = TypingColumns
            .Where(columns => loci.Contains(columns.Locus))
            .SelectMany(columns => new[] { columns.FirstPositionColumn, columns.SecondPositionColumn })
            .Select(column => $"d.{column}, NCHAR(31)");

        return $"HASHBYTES('SHA2_256', CONCAT({string.Join(", ", positions)}))";
    }

    private static string InsertGroupCountsSql(int keyOrdinal) => $"""
                                                                    INSERT INTO {BuildGroupRangesTableName} (PairId, KeyOrdinal, GroupCount)
                                                                    SELECT PairId, {keyOrdinal}, COUNT(DISTINCT {TypingHashColumn(keyOrdinal)})
                                                                    FROM {BuildDonorTypingsTableName}
                                                                    GROUP BY PairId
                                                                    """;

    /// <summary>
    /// The donors of the groups of one key. Within a pair, the rank of a typing among the distinct typings of the pair is
    /// the place of its group in the range of the pair and the key.
    /// </summary>
    private static string InsertGroupDonorsSql(int keyOrdinal) => $"""
                                                                    INSERT INTO {GroupDonorsTableName} WITH (TABLOCK) ({nameof(GroupDonor.GroupId)}, {nameof(GroupDonor.DonorId)})
                                                                    SELECT
                                                                        r.FirstGroupId - 1 + DENSE_RANK() OVER (PARTITION BY t.PairId ORDER BY t.{TypingHashColumn(keyOrdinal)}),
                                                                        t.DonorId
                                                                    FROM {BuildDonorTypingsTableName} t
                                                                    INNER JOIN {BuildGroupRangesTableName} r ON r.PairId = t.PairId AND r.KeyOrdinal = {keyOrdinal}
                                                                    """;

    public DonorGenotypePrecomputationRepository(IConnectionStringProvider connectionStringProvider) : base(connectionStringProvider)
    {
    }

    /// <inheritdoc />
    public async Task<Run?> GetRun(int dataRefreshRecordId)
    {
        await using var connection = await OpenConnection();
        var row = await connection.QuerySingleOrDefaultAsync<RunRow>(
            SelectRunSql,
            new { DataRefreshRecordId = dataRefreshRecordId },
            commandTimeout: CommandTimeoutInSeconds
        );

        return row?.ToEntity();
    }

    /// <inheritdoc />
    public async Task<Run> StartBuild(int dataRefreshRecordId, string hlaNomenclatureVersion, int groupsPerBatch)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(groupsPerBatch, 1);

        await using (var connection = await OpenConnection())
        {
            await using var transaction = await connection.BeginTransactionAsync();
            var parameters = new
            {
                DataRefreshRecordId = dataRefreshRecordId,
                HlaNomenclatureVersion = hlaNomenclatureVersion,
                GroupsPerBatch = groupsPerBatch
            };

            var run = await connection.QuerySingleOrDefaultAsync<RunToStartBuildRow>(
                SelectRunToStartBuildSql,
                parameters,
                transaction,
                CommandTimeoutInSeconds
            );

            if (run == null)
            {
                await connection.ExecuteAsync(InsertBuildingRunSql, parameters, transaction, CommandTimeoutInSeconds);
            }
            else if (run.Status == nameof(RunStatus.Building))
            {
                await connection.ExecuteAsync(RestartBuildSql, new { RunId = run.Id }, transaction, CommandTimeoutInSeconds);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Run {run.Id} of data refresh record {dataRefreshRecordId} is {run.Status}. Only a run that is building can start its build.");
            }

            await connection.ExecuteAsync(TruncateStagingTablesSql, transaction: transaction, commandTimeout: CommandTimeoutInSeconds);
            await transaction.CommitAsync();
        }

        return await GetRun(dataRefreshRecordId)
               ?? throw new InvalidOperationException($"The run of data refresh record {dataRefreshRecordId} is gone after the start of its build.");
    }

    /// <inheritdoc />
    public async Task<DonorGenotypePrecomputationBuildResult> BuildRun(int runId, CancellationToken cancellationToken)
    {
        // One connection for the whole build: a temp table is visible only to the session that made it.
        await using var connection = new SqlConnection(ConnectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        var build = new BuildSession(connection, cancellationToken);

        var run = await connection.QuerySingleOrDefaultAsync<RunToBuildRow>(new CommandDefinition(
                      SelectRunToBuildSql,
                      new { RunId = runId },
                      commandTimeout: CommandTimeoutInSeconds,
                      cancellationToken: cancellationToken))
                  ?? throw new InvalidOperationException($"Run {runId} does not exist.");
        if (run.Status != nameof(RunStatus.Building) || run.HasBatches || run.HasStagingData)
        {
            throw new InvalidOperationException(
                $"Run {runId} cannot be built: its status is {run.Status}, it has batches: {run.HasBatches}, there is staging data: " +
                $"{run.HasStagingData}. Call {nameof(StartBuild)} first.");
        }

        try
        {
            await build.Execute(CreateBuildTablesSql);
            await build.Step("Pairs", InsertPairsSql);

            var donorCount = await build.Step("Donor typings", InsertDonorTypingsSql);
            var donorsInTable = await build.Scalar<int>(CountDonorsSql);
            if (donorCount != donorsInTable)
            {
                throw new InvalidOperationException(
                    $"The build of run {runId} found the registry and ethnicity pair of {donorCount} of the {donorsInTable} donors.");
            }

            foreach (var keyOrdinal in BuildKeyOrdinals)
            {
                await build.Step($"Group counts {BuildKeys[keyOrdinal - 1]}", InsertGroupCountsSql(keyOrdinal));
            }

            await build.Step("Group ids", SetFirstGroupIdsSql);

            var donorAssignmentCount = 0;
            foreach (var keyOrdinal in BuildKeyOrdinals)
            {
                var key = BuildKeys[keyOrdinal - 1];
                var rowCount = await build.Step($"Group donors {key}", InsertGroupDonorsSql(keyOrdinal));
                if (rowCount != donorCount)
                {
                    throw new InvalidOperationException($"The build of run {runId} put {rowCount} of the {donorCount} donors in a group of {key}.");
                }

                donorAssignmentCount += rowCount;
            }

            var groupCount = await build.Step("Groups", InsertGroupsSql, new { RunId = runId });
            var countedGroupCount = await build.Scalar<int>(CountRangeGroupsSql);
            if (groupCount != countedGroupCount)
            {
                throw new InvalidOperationException($"The build of run {runId} wrote {groupCount} groups, and counted {countedGroupCount}.");
            }

            var batchCount = await build.Step("Batches", InsertBatchesSql, new { RunId = runId, run.GroupsPerBatch });

            var finishedRunCount = await build.Execute(FinishBuildSql, new
            {
                RunId = runId,
                TotalGroupCount = groupCount,
                TotalBatchCount = batchCount,
                TotalDonorAssignmentCount = donorAssignmentCount,
                TotalDonorCount = donorCount
            });
            if (finishedRunCount != 1)
            {
                throw new InvalidOperationException($"Run {runId} stopped building during its build.");
            }

            var status = batchCount == 0 ? RunStatus.Completed : RunStatus.Running;
            return new DonorGenotypePrecomputationBuildResult(status, donorCount, groupCount, batchCount, donorAssignmentCount, build.Steps);
        }
        finally
        {
            await build.DropTables();
        }
    }

    /// <inheritdoc />
    public async Task<ClaimedDonorGenotypePrecomputationBatch?> TryClaimBatch(DonorGenotypePrecomputationBatchClaim claim)
    {
        await using var connection = await OpenConnection();
        return await connection.QuerySingleOrDefaultAsync<ClaimedDonorGenotypePrecomputationBatch>(
            ClaimBatchSql,
            new
            {
                claim.BatchId,
                claim.RunId,
                claim.DataRefreshRecordId,
                claim.LeaseOwner,
                LeaseSeconds = (int)Math.Ceiling(claim.LeaseDuration.TotalSeconds),
                claim.IsRedelivery
            },
            commandTimeout: CommandTimeoutInSeconds
        );
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DonorGenotypePrecomputationGroupToCompute>> GetGroupsToCompute(int runId, int firstGroupId, int lastGroupId)
    {
        await using var connection = await OpenConnection();
        var rows = await connection.QueryAsync<GroupToComputeRow>(
            SelectGroupsToComputeSql,
            new { RunId = runId, FirstGroupId = firstGroupId, LastGroupId = lastGroupId },
            commandTimeout: CommandTimeoutInSeconds
        );

        return [.. rows.Select(row => row.ToModel())];
    }

    /// <inheritdoc />
    public async Task RecordGroupOutcomes(int runId, IReadOnlyCollection<Outcome> outcomes)
    {
        if (outcomes.Count == 0)
        {
            return;
        }

        var repeatedGroupId = outcomes.GroupBy(outcome => outcome.GroupId).FirstOrDefault(group => group.Count() > 1)?.Key;
        if (repeatedGroupId != null)
        {
            throw new ArgumentException($"Group {repeatedGroupId} has more than one outcome.", nameof(outcomes));
        }

        // One connection for the whole call: a local temp table is visible only to the session that made it.
        await using var connection = await OpenConnection();
        await connection.ExecuteAsync(CreateGroupOutcomeStagingTableSql, commandTimeout: CommandTimeoutInSeconds);
        await StageGroupOutcomes(connection, outcomes);
        await connection.ExecuteAsync(UpdateGroupsFromStagedOutcomesSql, new { RunId = runId }, commandTimeout: CommandTimeoutInSeconds);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DonorSubjectGenotypeSetAssignment>> GetDonorAssignments(int runId, int firstGroupId, int lastGroupId)
    {
        await using var connection = await OpenConnection();
        var rows = await connection.QueryAsync<DonorAssignmentRow>(
            SelectDonorAssignmentsSql,
            new { RunId = runId, FirstGroupId = firstGroupId, LastGroupId = lastGroupId },
            commandTimeout: CommandTimeoutInSeconds
        );

        return [.. rows.Select(row => row.ToModel())];
    }

    /// <inheritdoc />
    public async Task<bool> TryMarkBatchResultsReceived(int batchId, Guid leaseOwner, int failedGroupCount)
    {
        await using var connection = await OpenConnection();
        var rowsUpdated = await connection.ExecuteAsync(
            MarkBatchResultsReceivedSql,
            new { BatchId = batchId, LeaseOwner = leaseOwner, FailedGroupCount = failedGroupCount },
            commandTimeout: CommandTimeoutInSeconds
        );

        return rowsUpdated == 1;
    }

    /// <inheritdoc />
    public async Task<bool> TryMarkBatchFailed(int batchId, Guid leaseOwner, DonorGenotypePrecomputationBatchFailure failure)
    {
        await using var connection = await OpenConnection();
        var rowsUpdated = await connection.ExecuteAsync(
            MarkBatchFailedSql,
            new
            {
                BatchId = batchId,
                LeaseOwner = leaseOwner,
                FailureMessage = TruncateFailureMessage(failure.FailureMessage),
                failure.FailureException,
                failure.FailedGroupCount
            },
            commandTimeout: CommandTimeoutInSeconds
        );

        return rowsUpdated == 1;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PendingDonorGenotypePrecomputationBatch>> GetPendingBatches(
        int runId,
        PendingBatchSelection selection,
        int afterBatchId,
        int maxCount)
    {
        await using var connection = await OpenConnection();
        var batches = await connection.QueryAsync<PendingDonorGenotypePrecomputationBatch>(
            SelectPendingBatchesSql,
            new
            {
                RunId = runId,
                RequeuedOnly = selection == PendingBatchSelection.Requeued,
                AfterBatchId = afterBatchId,
                MaxCount = maxCount
            },
            commandTimeout: CommandTimeoutInSeconds
        );

        return [.. batches];
    }

    /// <inheritdoc />
    public async Task<int> MarkBatchesRequested(int runId, IReadOnlyCollection<PendingDonorGenotypePrecomputationBatch> batches)
    {
        if (batches.Count == 0)
        {
            return 0;
        }

        await using var connection = await OpenConnection();

        // One statement per retry count. The batches of one dispatch almost always share one count.
        var movedCount = 0;
        foreach (var batchesWithOneRetryCount in batches.Distinct().GroupBy(batch => batch.RetryCount))
        {
            foreach (var chunk in batchesWithOneRetryCount.Select(batch => batch.BatchId).Chunk(MaxIdsPerStatement))
            {
                movedCount += await connection.ExecuteAsync(
                    MarkBatchesRequestedSql,
                    new { RunId = runId, RetryCount = batchesWithOneRetryCount.Key, BatchIds = chunk },
                    commandTimeout: CommandTimeoutInSeconds
                );
            }
        }

        return movedCount;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SweptDonorGenotypePrecomputationBatch>> MarkExpiredBatchesAbandoned(int runId) =>
        Sweep(MarkExpiredBatchesAbandonedSql, new { RunId = runId, LeaseExpiredMessage });

    /// <inheritdoc />
    public Task<IReadOnlyList<SweptDonorGenotypePrecomputationBatch>> MarkBatchesPermanentlyFailed(int runId, int maxBatchRetries) =>
        Sweep(MarkBatchesPermanentlyFailedSql, new { RunId = runId, MaxBatchRetries = maxBatchRetries });

    /// <inheritdoc />
    public Task<IReadOnlyList<SweptDonorGenotypePrecomputationBatch>> RequeueRetryableBatches(int runId, int maxBatchRetries) =>
        Sweep(RequeueRetryableBatchesSql, new { RunId = runId, MaxBatchRetries = maxBatchRetries });

    /// <inheritdoc />
    public async Task<bool> TryMarkBatchAbandoned(int dataRefreshRecordId, int runId, int batchId, string reason)
    {
        await using var connection = await OpenConnection();
        var rowsUpdated = await connection.ExecuteAsync(
            MarkBatchAbandonedSql,
            new
            {
                DataRefreshRecordId = dataRefreshRecordId,
                RunId = runId,
                BatchId = batchId,
                Reason = TruncateFailureMessage(reason)
            },
            commandTimeout: CommandTimeoutInSeconds
        );

        return rowsUpdated == 1;
    }

    /// <inheritdoc />
    public async Task<RunStatus?> TryFinaliseRun(int runId)
    {
        await using var connection = await OpenConnection();
        var status = await connection.QuerySingleOrDefaultAsync<string>(
            FinaliseRunSql,
            new { RunId = runId },
            commandTimeout: CommandTimeoutInSeconds
        );

        return status == null ? null : Enum.Parse<RunStatus>(status);
    }

    /// <inheritdoc />
    public async Task<bool> TryMarkRunCancelled(int dataRefreshRecordId)
    {
        await using var connection = await OpenConnection();
        var rowsUpdated = await connection.ExecuteAsync(
            MarkRunCancelledSql,
            new { DataRefreshRecordId = dataRefreshRecordId },
            commandTimeout: CommandTimeoutInSeconds
        );

        return rowsUpdated == 1;
    }

    /// <inheritdoc />
    public async Task<DonorGenotypePrecomputationBatchCounts> GetBatchCounts(int runId)
    {
        await using var connection = await OpenConnection();
        var rows = (await connection.QueryAsync<BatchCountRow>(
            SelectBatchCountsSql,
            new { RunId = runId },
            commandTimeout: CommandTimeoutInSeconds
        )).ToList();

        return new DonorGenotypePrecomputationBatchCounts(
            rows.ToDictionary(row => Enum.Parse<BatchStatus>(row.Status), row => row.BatchCount),
            rows.Sum(row => row.FailedGroupCount)
        );
    }

    /// <inheritdoc />
    public async Task<DonorGenotypePrecomputationFailureSummary?> GetFailureSummary(int runId)
    {
        var parameters = new { RunId = runId, MaxSampleCount = MaxFailureSampleCount };

        await using var connection = await OpenConnection();
        var run = await connection.QuerySingleOrDefaultAsync<RunDonorCountRow>(
            SelectRunDonorCountSql,
            parameters,
            commandTimeout: CommandTimeoutInSeconds
        );
        if (run == null)
        {
            return null;
        }

        var counts = await connection.QuerySingleAsync<FailureCountsRow>(
            SelectFailureCountsSql,
            parameters,
            commandTimeout: FailureSummaryCommandTimeoutInSeconds
        );
        var samples = await connection.QueryAsync<FailureSampleRow>(
            SelectFailureSamplesSql,
            parameters,
            commandTimeout: FailureSummaryCommandTimeoutInSeconds
        );

        return new DonorGenotypePrecomputationFailureSummary(
            counts.PermanentlyFailedBatchCount,
            counts.FailedGroupCount,
            counts.FailedDonorCount,
            run.TotalDonorCount ?? 0,
            [.. samples.Select(sample => sample.ToModel())]
        );
    }

    /// <inheritdoc />
    public async Task<int> ResetFailedBatchesForManualRetry(int runId)
    {
        await using var connection = await OpenConnection();
        await using var transaction = await connection.BeginTransactionAsync();

        var runsReset = await connection.ExecuteAsync(ResetRunForManualRetrySql, new { RunId = runId }, transaction, CommandTimeoutInSeconds);
        if (runsReset == 0)
        {
            return 0;
        }

        var batchesReset = await connection.ExecuteAsync(ResetFailedBatchesSql, new { RunId = runId }, transaction, CommandTimeoutInSeconds);

        // A run with nothing to retry stays as it was: the dispose rolls back its move to running.
        if (batchesReset == 0)
        {
            return 0;
        }

        await transaction.CommitAsync();
        return batchesReset;
    }

    /// <inheritdoc />
    public async Task TruncateStagingTables()
    {
        await using var connection = await OpenConnection();
        await connection.ExecuteAsync(TruncateStagingTablesSql, commandTimeout: CommandTimeoutInSeconds);
    }

    /// <inheritdoc />
    public async Task<bool> HasStagingData()
    {
        await using var connection = await OpenConnection();
        return await connection.ExecuteScalarAsync<bool>(HasStagingDataSql, commandTimeout: CommandTimeoutInSeconds);
    }

    [return: NotNullIfNotNull(nameof(message))]
    internal static string? TruncateFailureMessage(string? message) =>
        message?.Length > FailureMessageMaxLength ? message[..FailureMessageMaxLength] : message;

    private async Task<SqlConnection> OpenConnection()
    {
        var connection = new SqlConnection(ConnectionStringProvider.GetConnectionString());
        await connection.OpenAsync();
        return connection;
    }

    private async Task<IReadOnlyList<SweptDonorGenotypePrecomputationBatch>> Sweep(string sql, object parameters)
    {
        await using var connection = await OpenConnection();
        var rows = await connection.QueryAsync<SweptBatchRow>(sql, parameters, commandTimeout: CommandTimeoutInSeconds);

        return [.. rows.Select(row => row.ToModel())];
    }

    /// <summary>
    /// Bulk-copies the outcomes into the temp table, over the caller's connection. The column types are declared: the
    /// <c>Columns.Add("Name")</c> form makes string columns, and an <c>int</c> column must not be written as text.
    /// </summary>
    private static async Task StageGroupOutcomes(SqlConnection connection, IReadOnlyCollection<Outcome> outcomes)
    {
        var dataTable = new DataTable();
        dataTable.Columns.Add(new DataColumn(nameof(Outcome.GroupId), typeof(int)));
        dataTable.Columns.Add(new DataColumn(nameof(Outcome.SubjectGenotypeSetValueId), typeof(int)));
        dataTable.Columns.Add(new DataColumn(nameof(Outcome.FailureMessage), typeof(string)));

        foreach (var outcome in outcomes)
        {
            dataTable.Rows.Add(
                outcome.GroupId,
                (object?)outcome.SubjectGenotypeSetValueId ?? DBNull.Value,
                (object?)TruncateFailureMessage(outcome.FailureMessage) ?? DBNull.Value
            );
        }

        using var bulkCopy = new SqlBulkCopy(connection);
        bulkCopy.BulkCopyTimeout = CommandTimeoutInSeconds;
        bulkCopy.DestinationTableName = GroupOutcomeStagingTableName;
        bulkCopy.ColumnMappings.Add(nameof(Outcome.GroupId), nameof(Outcome.GroupId));
        bulkCopy.ColumnMappings.Add(nameof(Outcome.SubjectGenotypeSetValueId), nameof(Outcome.SubjectGenotypeSetValueId));
        bulkCopy.ColumnMappings.Add(nameof(Outcome.FailureMessage), nameof(Outcome.FailureMessage));

        await bulkCopy.WriteToServerAsync(dataTable);
    }

    // The row classes below read each enum column as the string it is stored as, and parse it. The columns hold member
    // names (see SearchAlgorithmContext), and a map straight onto an enum property would depend on the conversion rules
    // of Dapper for a column whose type is not the type of the property.

    private sealed class RunRow
    {
        public int Id { get; init; }

        public int DataRefreshRecordId { get; init; }

        public required string HlaNomenclatureVersion { get; init; }

        public required string Status { get; init; }

        public int GroupsPerBatch { get; init; }

        public int? TotalGroupCount { get; init; }

        public int? TotalBatchCount { get; init; }

        public int? TotalDonorAssignmentCount { get; init; }

        public int? TotalDonorCount { get; init; }

        public int ManualRetryCount { get; init; }

        public DateTime CreatedUtc { get; init; }

        public DateTime StatusDateUtc { get; init; }

        public DateTime? CompletedUtc { get; init; }

        public Run ToEntity() => new()
        {
            Id = Id,
            DataRefreshRecordId = DataRefreshRecordId,
            HlaNomenclatureVersion = HlaNomenclatureVersion,
            Status = Enum.Parse<RunStatus>(Status),
            GroupsPerBatch = GroupsPerBatch,
            TotalGroupCount = TotalGroupCount,
            TotalBatchCount = TotalBatchCount,
            TotalDonorAssignmentCount = TotalDonorAssignmentCount,
            TotalDonorCount = TotalDonorCount,
            ManualRetryCount = ManualRetryCount,
            CreatedUtc = CreatedUtc,
            StatusDateUtc = StatusDateUtc,
            CompletedUtc = CompletedUtc
        };
    }

    private sealed class GroupToComputeRow
    {
        public int GroupId { get; init; }

        public required string AllowedLociKey { get; init; }

        public int RepresentativeDonorId { get; init; }

        public int? FoundDonorId { get; init; }

        public string? RegistryCode { get; init; }

        public string? EthnicityCode { get; init; }

        public string? A_1 { get; init; }

        public string? A_2 { get; init; }

        public string? B_1 { get; init; }

        public string? B_2 { get; init; }

        public string? C_1 { get; init; }

        public string? C_2 { get; init; }

        public string? DPB1_1 { get; init; }

        public string? DPB1_2 { get; init; }

        public string? DQB1_1 { get; init; }

        public string? DQB1_2 { get; init; }

        public string? DRB1_1 { get; init; }

        public string? DRB1_2 { get; init; }

        public DonorGenotypePrecomputationGroupToCompute ToModel() => new(
            GroupId,
            Enum.Parse<AllowedLociKey>(AllowedLociKey),
            RepresentativeDonorId,
            RegistryCode,
            EthnicityCode,
            FoundDonorId == null
                ? null
                : new PhenotypeInfo<string?>(
                    valueA: new LocusInfo<string?>(A_1, A_2),
                    valueB: new LocusInfo<string?>(B_1, B_2),
                    valueC: new LocusInfo<string?>(C_1, C_2),
                    valueDpb1: new LocusInfo<string?>(DPB1_1, DPB1_2),
                    valueDqb1: new LocusInfo<string?>(DQB1_1, DQB1_2),
                    valueDrb1: new LocusInfo<string?>(DRB1_1, DRB1_2)
                )
        );
    }

    private sealed class DonorAssignmentRow
    {
        public int DonorId { get; init; }

        public required string AllowedLociKey { get; init; }

        public int SubjectGenotypeSetValueId { get; init; }

        public DonorSubjectGenotypeSetAssignment ToModel() =>
            new(DonorId, Enum.Parse<AllowedLociKey>(AllowedLociKey), SubjectGenotypeSetValueId);
    }

    private sealed class SweptBatchRow
    {
        public int BatchId { get; init; }

        public required string PreviousStatus { get; init; }

        public int RetryCount { get; init; }

        public string? FailureMessage { get; init; }

        public SweptDonorGenotypePrecomputationBatch ToModel() => new(
            BatchId,
            Enum.Parse<BatchStatus>(PreviousStatus),
            RetryCount,
            FailureMessage
        );
    }

    private sealed class BatchCountRow
    {
        public required string Status { get; init; }

        public int BatchCount { get; init; }

        public int FailedGroupCount { get; init; }
    }

    private sealed class RunDonorCountRow
    {
        public int Id { get; init; }

        public int? TotalDonorCount { get; init; }
    }

    private sealed class FailureCountsRow
    {
        public int PermanentlyFailedBatchCount { get; init; }

        public int FailedGroupCount { get; init; }

        public int FailedDonorCount { get; init; }
    }

    private sealed class FailureSampleRow
    {
        public int BatchId { get; init; }

        public int? GroupId { get; init; }

        public string? FailureMessage { get; init; }

        public DonorGenotypePrecomputationFailureSample ToModel() => new(BatchId, GroupId, FailureMessage);
    }

    private sealed class RunToStartBuildRow
    {
        public int Id { get; init; }

        public required string Status { get; init; }
    }

    private sealed class RunToBuildRow
    {
        public required string Status { get; init; }

        public int GroupsPerBatch { get; init; }

        public bool HasBatches { get; init; }

        public bool HasStagingData { get; init; }
    }

    /// <summary>
    /// The connection of one build, and the record of its steps. Every command has the long timeout of the build, and the
    /// token of the caller.
    /// </summary>
    private sealed class BuildSession(SqlConnection connection, CancellationToken cancellationToken)
    {
        private readonly List<DonorGenotypePrecomputationBuildStep> steps = [];

        public IReadOnlyList<DonorGenotypePrecomputationBuildStep> Steps => steps;

        /// <summary>Runs one step of the build, and records the rows that it wrote and its duration.</summary>
        public async Task<int> Step(string name, string sql, object? parameters = null)
        {
            var startTimestamp = Stopwatch.GetTimestamp();
            var rowCount = await Execute(sql, parameters);
            steps.Add(new DonorGenotypePrecomputationBuildStep(name, rowCount, Stopwatch.GetElapsedTime(startTimestamp)));
            return rowCount;
        }

        public Task<int> Execute(string sql, object? parameters = null) => connection.ExecuteAsync(Command(sql, parameters));

        public Task<T?> Scalar<T>(string sql) => connection.ExecuteScalarAsync<T>(Command(sql));

        /// <summary>
        /// Drops the temp tables of the build: for a full donor set they take a few GB of tempdb. A closed connection goes back
        /// to the pool with its session, and its temp tables stay until the pool resets or closes the connection.
        /// </summary>
        /// <remarks>
        /// For a <c>finally</c> block, so it does not throw: an error here would hide the error that stopped the build. The
        /// session drops the tables anyway when it ends.
        /// </remarks>
        public async Task DropTables()
        {
            if (connection.State != ConnectionState.Open)
            {
                return;
            }

            try
            {
                await connection.ExecuteAsync(DropBuildTablesSql, commandTimeout: CommandTimeoutInSeconds);
            }
            catch (Exception exception) when (exception is SqlException or InvalidOperationException)
            {
                // The error that stopped the build, if there was one, is the error to report.
            }
        }

        private CommandDefinition Command(string sql, object? parameters = null) =>
            new(sql, parameters, commandTimeout: BuildCommandTimeoutInSeconds, cancellationToken: cancellationToken);
    }
}