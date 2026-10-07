#nullable enable

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
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
}