using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using BatchStatus = Atlas.MatchingAlgorithm.Data.Models.Entities.DonorGenotypePrecomputationBatchStatus;
using ContextFactory = Atlas.MatchingAlgorithm.Data.Context.ContextFactory;
using Injection = Atlas.MatchingAlgorithm.Test.Integration.DependencyInjection.DependencyInjection;
using Outcome = Atlas.MatchingAlgorithm.Data.Models.Precompute.DonorGenotypePrecomputationGroupOutcome;
using RunStatus = Atlas.MatchingAlgorithm.Data.Models.Entities.DonorGenotypePrecomputationRunStatus;
using SearchAlgorithmContext = Atlas.MatchingAlgorithm.Data.Context.SearchAlgorithmContext;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Precompute;

/// <summary>
/// The precomputation repository against a real database: the compare-and-swap rules of the batch state machine, and the reads
/// and writes that a worker makes for one batch.
/// </summary>
[TestFixture]
public class DonorGenotypePrecomputationRepositoryTests
{
    private const int MaxBatchRetries = 3;
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(30);

    /// <summary>The database sets the timestamps, and the test reads the clock of the same machine.</summary>
    private static readonly TimeSpan ClockTolerance = TimeSpan.FromMinutes(1);

    private Fixture fixture;
    private StaticallyChosenTransientSqlConnectionStringProviderFactory connectionStringFactory;
    private IStaticallyChosenDatabaseRepositoryFactory repositoryFactory;
    private IDonorGenotypePrecomputationRepository repository;
    private int nextBatchNumber;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        nextBatchNumber = 0;

        connectionStringFactory = Injection.Provider.GetService<StaticallyChosenTransientSqlConnectionStringProviderFactory>();
        repositoryFactory = Injection.Provider.GetService<IStaticallyChosenDatabaseRepositoryFactory>();
        repository = repositoryFactory.GetDonorGenotypePrecomputationRepositoryForDatabase(TransientDatabase.DatabaseA);

        DatabaseManager.ClearTransientDatabases();
    }

    [Test]
    public async Task GetRun_ForTheRecordOfARun_ReturnsTheRun()
    {
        var run = await InsertRun();

        var result = await repository.GetRun(run.DataRefreshRecordId);

        result.Should().BeEquivalentTo(run);
    }

    [Test]
    public async Task GetRun_WhenTheRecordHasNoRun_ReturnsNull()
    {
        await InsertRun();

        var result = await repository.GetRun(fixture.Create<int>());

        result.Should().BeNull();
    }

    [Test]
    public async Task GetDonorGenotypePrecomputationRepositoryForDatabase_ReadsTheDatabaseItIsGiven()
    {
        // The worker takes its database from the refresh record. A repository that read the other one would find no run,
        // and skip every batch of the refresh.
        var run = await InsertRun(database: TransientDatabase.DatabaseB);

        var fromB = await repositoryFactory.GetDonorGenotypePrecomputationRepositoryForDatabase(TransientDatabase.DatabaseB)
            .GetRun(run.DataRefreshRecordId);
        var fromA = await repository.GetRun(run.DataRefreshRecordId);

        fromB.Should().NotBeNull();
        fromA.Should().BeNull();
    }

    [TestCase(BatchStatus.Pending)]
    [TestCase(BatchStatus.Requested)]
    public async Task TryClaimBatch_ForABatchNotYetClaimed_TakesItAndReturnsItsRangeAndTheRunVersion(BatchStatus status)
    {
        var run = await InsertRun();
        var batch = await InsertBatch(run.Id, status);
        var claim = NewClaim(run, batch);

        var claimed = await repository.TryClaimBatch(claim);

        claimed.Should().BeEquivalentTo(new ClaimedDonorGenotypePrecomputationBatch(
            batch.Id, run.Id, batch.FirstGroupId, batch.LastGroupId, batch.RetryCount, run.HlaNomenclatureVersion));
        var stored = await StoredBatch(batch.Id);
        stored.Status.Should().Be(BatchStatus.InProgress);
        stored.LeaseOwner.Should().Be(claim.LeaseOwner);
        stored.LeaseExpiresUtc.Should().BeCloseTo(DateTime.UtcNow + LeaseDuration, ClockTolerance);
    }

    [TestCase(RunStatus.Building)]
    [TestCase(RunStatus.Completed)]
    [TestCase(RunStatus.CompletedWithFailures)]
    [TestCase(RunStatus.Cancelled)]
    public async Task TryClaimBatch_WhenTheRunIsNotRunning_ReturnsNullAndLeavesTheBatch(RunStatus runStatus)
    {
        var run = await InsertRun(runStatus);
        var batch = await InsertBatch(run.Id);

        var claimed = await repository.TryClaimBatch(NewClaim(run, batch));

        claimed.Should().BeNull();
        (await StoredBatch(batch.Id)).Status.Should().Be(BatchStatus.Pending);
    }

    [Test]
    public async Task TryClaimBatch_ForTheRunOfAnotherRefresh_ReturnsNull()
    {
        // A message from an earlier refresh. Run ids repeat across refreshes, because each refresh truncates the tables,
        // so the record id is what tells the two runs apart.
        var run = await InsertRun();
        var batch = await InsertBatch(run.Id);

        var claimed = await repository.TryClaimBatch(NewClaim(run, batch) with { DataRefreshRecordId = fixture.Create<int>() });

        claimed.Should().BeNull();
        (await StoredBatch(batch.Id)).Status.Should().Be(BatchStatus.Pending);
    }

    [TestCase(BatchStatus.ResultsReceived)]
    [TestCase(BatchStatus.PermanentlyFailed)]
    [TestCase(BatchStatus.Failed)]
    [TestCase(BatchStatus.Abandoned)]
    public async Task TryClaimBatch_ForABatchThatNoMessageIsFor_ReturnsNullAndLeavesTheBatch(BatchStatus status)
    {
        // Terminal batches are done. A failed or abandoned batch waits for a sweep, which sends a new message for it.
        var run = await InsertRun();
        var batch = await InsertBatch(run.Id, status);

        var claimed = await repository.TryClaimBatch(NewClaim(run, batch));

        claimed.Should().BeNull();
        (await StoredBatch(batch.Id)).Status.Should().Be(status);
    }

    [Test]
    public async Task TryClaimBatch_ForABatchUnderALiveLease_ReturnsNullAndKeepsTheLease()
    {
        // A second copy of a published message: the batch is dispatched again while its first worker is still on it.
        var run = await InsertRun();
        var batch = await InsertBatch(run.Id);
        var firstClaim = NewClaim(run, batch);
        await repository.TryClaimBatch(firstClaim);

        var claimed = await repository.TryClaimBatch(NewClaim(run, batch));

        claimed.Should().BeNull();
        (await StoredBatch(batch.Id)).LeaseOwner.Should().Be(firstClaim.LeaseOwner);
    }

    [Test]
    public async Task TryClaimBatch_ForARedeliveredMessage_TakesTheBatchFromALiveLease()
    {
        // Service Bus delivers a message again only when its worker stopped without completing it, so the live lease has
        // no worker behind it, and waiting for it to expire would only lose time.
        var run = await InsertRun();
        var batch = await InsertBatch(run.Id);
        await repository.TryClaimBatch(NewClaim(run, batch));
        var redelivery = NewClaim(run, batch) with { IsRedelivery = true };

        var claimed = await repository.TryClaimBatch(redelivery);

        claimed.Should().NotBeNull();
        (await StoredBatch(batch.Id)).LeaseOwner.Should().Be(redelivery.LeaseOwner);
    }

    [Test]
    public async Task TryClaimBatch_ForABatchWhoseLeaseExpired_TakesIt()
    {
        var run = await InsertRun();
        var batch = await InsertBatch(run.Id, BatchStatus.InProgress, b =>
        {
            b.LeaseOwner = fixture.Create<Guid>();
            b.LeaseExpiresUtc = DateTime.UtcNow.AddHours(-1);
        });
        var claim = NewClaim(run, batch);

        var claimed = await repository.TryClaimBatch(claim);

        claimed.Should().NotBeNull();
        (await StoredBatch(batch.Id)).LeaseOwner.Should().Be(claim.LeaseOwner);
    }

    [Test]
    public async Task TryClaimBatch_ConcurrentlyForOneBatch_LetsExactlyOneClaimSucceed()
    {
        // Eight copies of one message, each call on its own connection. The compare-and-swap is the only thing that keeps
        // two workers off one batch.
        var run = await InsertRun();
        var batch = await InsertBatch(run.Id);
        var claims = Enumerable.Range(0, 8).Select(_ => NewClaim(run, batch)).ToList();

        var results = await Task.WhenAll(claims.Select(claim => repository.TryClaimBatch(claim)));

        results.Where(result => result != null).Should().ContainSingle();
        var winner = claims[Array.FindIndex(results, result => result != null)];
        (await StoredBatch(batch.Id)).LeaseOwner.Should().Be(winner.LeaseOwner);
    }

    [Test]
    public async Task GetGroupsToCompute_ReturnsTheGroupsOfTheRangeThatHaveNoValue_InIdOrder()
    {
        var run = await InsertRun();
        var donor = await InsertDonor();
        foreach (var groupId in new[] { 1, 2, 3, 4, 5 })
        {
            await InsertGroup(run.Id, groupId, donor.DonorId, subjectGenotypeSetValueId: groupId == 3 ? fixture.Create<int>() : null);
        }

        var groups = await repository.GetGroupsToCompute(run.Id, firstGroupId: 2, lastGroupId: 4);

        groups.Select(group => group.GroupId).Should().Equal(2, 4);
    }

    [Test]
    public async Task GetGroupsToCompute_GivesEachGroupTheTypingAndTheCodesOfItsRepresentativeDonor()
    {
        var run = await InsertRun();
        var donor = await InsertDonor();
        var group = await InsertGroup(run.Id, 1, donor.DonorId);

        var groups = await repository.GetGroupsToCompute(run.Id, 1, 1);

        groups.Should().ContainSingle().Which.Should().BeEquivalentTo(new DonorGenotypePrecomputationGroupToCompute(
            group.Id,
            group.AllowedLociKey,
            donor.DonorId,
            donor.RegistryCode,
            donor.EthnicityCode,
            donor.ToDonorInfo().HlaNames));
    }

    [Test]
    public async Task GetGroupsToCompute_WhenTheRepresentativeDonorIsMissing_ReturnsTheGroupWithNoTyping()
    {
        // A missing donor fails its own group. It must not make the read fail, or it would fail every group of the batch.
        var run = await InsertRun();
        await InsertGroup(run.Id, 1, representativeDonorId: fixture.Create<int>());

        var groups = await repository.GetGroupsToCompute(run.Id, 1, 1);

        groups.Should().ContainSingle().Which.HlaTyping.Should().BeNull();
    }

    [Test]
    public async Task GetGroupsToCompute_IgnoresTheGroupsOfAnotherRun()
    {
        var run = await InsertRun();
        var otherRun = await InsertRun();
        var donor = await InsertDonor();
        await InsertGroup(otherRun.Id, 1, donor.DonorId);

        var groups = await repository.GetGroupsToCompute(run.Id, 1, 1);

        groups.Should().BeEmpty();
    }

    [Test]
    public async Task RecordGroupOutcomes_StoresTheValueOrTheFailureOfEachGroup()
    {
        var run = await InsertRun();
        await InsertGroup(run.Id, 1, fixture.Create<int>());
        await InsertGroup(run.Id, 2, fixture.Create<int>());
        var valueId = fixture.Create<int>();
        var failureMessage = fixture.Create<string>();

        await repository.RecordGroupOutcomes(run.Id, [Outcome.Stored(1, valueId), Outcome.Failed(2, failureMessage)]);

        var groups = await StoredGroups();
        groups[1].SubjectGenotypeSetValueId.Should().Be(valueId);
        groups[1].FailureMessage.Should().BeNull();
        groups[2].SubjectGenotypeSetValueId.Should().BeNull();
        groups[2].FailureMessage.Should().Be(failureMessage);
    }

    [Test]
    public async Task RecordGroupOutcomes_ForAGroupThatFailedBefore_StoresTheValueAndClearsTheFailure()
    {
        // A batch that runs again computes its failed groups again. One that succeeds this time is no longer a failure.
        var run = await InsertRun();
        await InsertGroup(run.Id, 1, fixture.Create<int>(), failureMessage: fixture.Create<string>());
        var valueId = fixture.Create<int>();

        await repository.RecordGroupOutcomes(run.Id, [Outcome.Stored(1, valueId)]);

        var group = (await StoredGroups())[1];
        group.SubjectGenotypeSetValueId.Should().Be(valueId);
        group.FailureMessage.Should().BeNull();
    }

    [Test]
    public async Task RecordGroupOutcomes_ForAGroupThatHasAValue_KeepsTheValueAndRecordsNoFailure()
    {
        // Two attempts at one batch can run at the same time. The attempt that fails a group must not undo the attempt
        // that stored its value.
        var run = await InsertRun();
        var valueId = fixture.Create<int>();
        await InsertGroup(run.Id, 1, fixture.Create<int>(), subjectGenotypeSetValueId: valueId);

        await repository.RecordGroupOutcomes(run.Id, [Outcome.Failed(1, fixture.Create<string>())]);

        var group = (await StoredGroups())[1];
        group.SubjectGenotypeSetValueId.Should().Be(valueId);
        group.FailureMessage.Should().BeNull();
    }

    [Test]
    public async Task RecordGroupOutcomes_WithAFailureMessageLongerThanItsColumn_StoresItsStart()
    {
        // An exception message has no length limit. Without the truncation, the update would fail and lose every outcome
        // of the batch.
        var run = await InsertRun();
        await InsertGroup(run.Id, 1, fixture.Create<int>());
        var longMessage = string.Concat(fixture.CreateMany<string>(20));

        await repository.RecordGroupOutcomes(run.Id, [Outcome.Failed(1, longMessage)]);

        (await StoredGroups())[1].FailureMessage.Should().Be(longMessage[..DonorGenotypePrecomputationRepository.FailureMessageMaxLength]);
    }

    [Test]
    public async Task RecordGroupOutcomes_WithTwoOutcomesForOneGroup_ThrowsAndStoresNothing()
    {
        var run = await InsertRun();
        await InsertGroup(run.Id, 1, fixture.Create<int>());

        var act = () => repository.RecordGroupOutcomes(run.Id, [Outcome.Stored(1, fixture.Create<int>()), Outcome.Failed(1, fixture.Create<string>())]);

        await act.Should().ThrowAsync<ArgumentException>();
        (await StoredGroups())[1].SubjectGenotypeSetValueId.Should().BeNull();
    }

    [Test]
    public async Task GetDonorAssignments_ReturnsEveryDonorOfEachGroupInTheRangeThatHasAValue()
    {
        var run = await InsertRun();
        var storedGroup = await InsertGroup(run.Id, 1, fixture.Create<int>(), subjectGenotypeSetValueId: fixture.Create<int>());
        var storedGroupDonorIds = await InsertGroupDonors(storedGroup.Id, 3);
        var groupWithNoValue = await InsertGroup(run.Id, 2, fixture.Create<int>());
        await InsertGroupDonors(groupWithNoValue.Id, 2);
        var groupOutsideTheRange = await InsertGroup(run.Id, 3, fixture.Create<int>(), subjectGenotypeSetValueId: fixture.Create<int>());
        await InsertGroupDonors(groupOutsideTheRange.Id, 2);

        var assignments = await repository.GetDonorAssignments(run.Id, 1, 2);

        assignments.Should().BeEquivalentTo(storedGroupDonorIds.Select(donorId =>
            new DonorSubjectGenotypeSetAssignment(donorId, storedGroup.AllowedLociKey, storedGroup.SubjectGenotypeSetValueId.Value)));
    }

    [Test]
    public async Task TryMarkBatchResultsReceived_ByTheLeaseOwner_CompletesTheBatchAndClearsItsLastFailure()
    {
        var run = await InsertRun();
        var batch = await InsertBatch(run.Id, BatchStatus.Pending, b =>
        {
            b.RetryCount = 1;
            b.FailureMessage = fixture.Create<string>();
            b.FailureException = fixture.Create<string>();
        });
        var claim = NewClaim(run, batch);
        await repository.TryClaimBatch(claim);
        var failedGroupCount = fixture.Create<int>();

        var completed = await repository.TryMarkBatchResultsReceived(batch.Id, claim.LeaseOwner, failedGroupCount);

        completed.Should().BeTrue();
        var stored = await StoredBatch(batch.Id);
        stored.Status.Should().Be(BatchStatus.ResultsReceived);
        stored.FailedGroupCount.Should().Be(failedGroupCount);
        stored.FailureMessage.Should().BeNull();
        stored.FailureException.Should().BeNull();
        stored.CompletedUtc.Should().BeCloseTo(DateTime.UtcNow, ClockTolerance);
    }

    [Test]
    public async Task TryMarkBatchResultsReceived_ByAWorkerThatDoesNotHoldTheLease_ReturnsFalseAndLeavesTheBatch()
    {
        var run = await InsertRun();
        var batch = await InsertBatch(run.Id);
        await repository.TryClaimBatch(NewClaim(run, batch));

        var completed = await repository.TryMarkBatchResultsReceived(batch.Id, fixture.Create<Guid>(), fixture.Create<int>());

        completed.Should().BeFalse();
        (await StoredBatch(batch.Id)).Status.Should().Be(BatchStatus.InProgress);
    }

    [Test]
    public async Task TryMarkBatchResultsReceived_AfterASweepAbandonedTheExpiredLease_LetsTheOwnerFinish()
    {
        // The worker was slow, not dead. Its result is complete, so it must count.
        var run = await InsertRun();
        var leaseOwner = fixture.Create<Guid>();
        var batch = await InsertBatch(run.Id, BatchStatus.InProgress, b =>
        {
            b.LeaseOwner = leaseOwner;
            b.LeaseExpiresUtc = DateTime.UtcNow.AddHours(-1);
        });
        await repository.MarkExpiredBatchesAbandoned(run.Id);

        var completed = await repository.TryMarkBatchResultsReceived(batch.Id, leaseOwner, fixture.Create<int>());

        completed.Should().BeTrue();
        (await StoredBatch(batch.Id)).Status.Should().Be(BatchStatus.ResultsReceived);
    }

    [Test]
    public async Task TryMarkBatchResultsReceived_AfterASweepSentTheBatchAgain_ReturnsFalse()
    {
        // From the requeue on, the next claim owns the batch. The old worker's result is still correct - the writes are
        // idempotent - but only one worker may say that the batch is done.
        var run = await InsertRun();
        var leaseOwner = fixture.Create<Guid>();
        var batch = await InsertBatch(run.Id, BatchStatus.InProgress, b =>
        {
            b.LeaseOwner = leaseOwner;
            b.LeaseExpiresUtc = DateTime.UtcNow.AddHours(-1);
        });
        await repository.MarkExpiredBatchesAbandoned(run.Id);
        await repository.RequeueRetryableBatches(run.Id, MaxBatchRetries);

        var completed = await repository.TryMarkBatchResultsReceived(batch.Id, leaseOwner, fixture.Create<int>());

        completed.Should().BeFalse();
        (await StoredBatch(batch.Id)).Status.Should().Be(BatchStatus.Pending);
    }

    [Test]
    public async Task TryMarkBatchFailed_ByTheLeaseOwner_RecordsTheFailure()
    {
        var run = await InsertRun();
        var batch = await InsertBatch(run.Id);
        var claim = NewClaim(run, batch);
        await repository.TryClaimBatch(claim);
        var failure = fixture.Create<DonorGenotypePrecomputationBatchFailure>();

        var failed = await repository.TryMarkBatchFailed(batch.Id, claim.LeaseOwner, failure);

        failed.Should().BeTrue();
        var stored = await StoredBatch(batch.Id);
        stored.Status.Should().Be(BatchStatus.Failed);
        stored.FailureMessage.Should().Be(failure.FailureMessage);
        stored.FailureException.Should().Be(failure.FailureException);
        stored.FailedGroupCount.Should().Be(failure.FailedGroupCount);
    }

    [Test]
    public async Task TryMarkBatchFailed_ByAWorkerThatDoesNotHoldTheLease_ReturnsFalseAndLeavesTheBatch()
    {
        var run = await InsertRun();
        var batch = await InsertBatch(run.Id);
        await repository.TryClaimBatch(NewClaim(run, batch));

        var failed = await repository.TryMarkBatchFailed(batch.Id, fixture.Create<Guid>(), fixture.Create<DonorGenotypePrecomputationBatchFailure>());

        failed.Should().BeFalse();
        (await StoredBatch(batch.Id)).Status.Should().Be(BatchStatus.InProgress);
    }

    [Test]
    public async Task GetPendingBatchIds_ReturnsThePendingBatchesAfterTheBound_InIdOrder_UpToTheMaximum()
    {
        var run = await InsertRun();
        var pendingBatches = new List<DonorGenotypePrecomputationBatch>();
        for (var i = 0; i < 5; i++)
        {
            pendingBatches.Add(await InsertBatch(run.Id));
        }

        await InsertBatch(run.Id, BatchStatus.Requested);
        await InsertBatch((await InsertRun()).Id);

        var ids = await repository.GetPendingBatchIds(run.Id, afterBatchId: pendingBatches[0].Id, maxCount: 3);

        ids.Should().Equal(pendingBatches.Skip(1).Take(3).Select(batch => batch.Id));
    }

    [Test]
    public async Task MarkBatchesRequested_MovesOnlyThePendingBatchesItIsGiven()
    {
        var run = await InsertRun();
        var pending = await InsertBatch(run.Id);
        var pendingNotGiven = await InsertBatch(run.Id);
        var claimedMeanwhile = await InsertBatch(run.Id, BatchStatus.InProgress);

        var movedCount = await repository.MarkBatchesRequested(run.Id, [pending.Id, claimedMeanwhile.Id]);

        movedCount.Should().Be(1);
        var storedPending = await StoredBatch(pending.Id);
        storedPending.Status.Should().Be(BatchStatus.Requested);
        storedPending.DispatchedUtc.Should().BeCloseTo(DateTime.UtcNow, ClockTolerance);
        (await StoredBatch(pendingNotGiven.Id)).Status.Should().Be(BatchStatus.Pending);
        (await StoredBatch(claimedMeanwhile.Id)).Status.Should().Be(BatchStatus.InProgress);
    }

    [Test]
    public async Task MarkBatchesRequested_ForMoreBatchesThanOneStatementTakes_MovesThemAll()
    {
        // SQL Server allows 2,100 parameters in one command, and a caller can pass any number of ids.
        var run = await InsertRun();
        var batchIds = await InsertPendingBatches(run.Id, DonorGenotypePrecomputationRepository.MaxIdsPerStatement + 5);

        var movedCount = await repository.MarkBatchesRequested(run.Id, batchIds);

        movedCount.Should().Be(batchIds.Count);
    }

    [Test]
    public async Task MarkExpiredBatchesAbandoned_MovesOnlyTheInProgressBatchesWhoseLeaseExpired()
    {
        var run = await InsertRun();
        var leaseOwner = fixture.Create<Guid>();
        var expired = await InsertBatch(run.Id, BatchStatus.InProgress, b =>
        {
            b.LeaseOwner = leaseOwner;
            b.LeaseExpiresUtc = DateTime.UtcNow.AddHours(-1);
        });
        var live = await InsertBatch(run.Id, BatchStatus.InProgress, b => b.LeaseExpiresUtc = DateTime.UtcNow.AddHours(1));
        var requested = await InsertBatch(run.Id, BatchStatus.Requested, b => b.LeaseExpiresUtc = DateTime.UtcNow.AddHours(-1));

        var swept = await repository.MarkExpiredBatchesAbandoned(run.Id);

        swept.Should().ContainSingle().Which.Should().BeEquivalentTo(new { BatchId = expired.Id, PreviousStatus = BatchStatus.InProgress });
        var storedExpired = await StoredBatch(expired.Id);
        storedExpired.Status.Should().Be(BatchStatus.Abandoned);
        storedExpired.FailureMessage.Should().Be(DonorGenotypePrecomputationRepository.LeaseExpiredMessage);
        storedExpired.LeaseOwner.Should().Be(leaseOwner);
        (await StoredBatch(live.Id)).Status.Should().Be(BatchStatus.InProgress);
        (await StoredBatch(requested.Id)).Status.Should().Be(BatchStatus.Requested);
    }

    [Test]
    public async Task MarkBatchesPermanentlyFailed_MovesTheBatchesWithNoRetriesLeft()
    {
        var run = await InsertRun();
        var failedWithNoRetriesLeft = await InsertBatch(run.Id, BatchStatus.Failed, b => b.RetryCount = MaxBatchRetries);
        var abandonedWithNoRetriesLeft = await InsertBatch(run.Id, BatchStatus.Abandoned, b => b.RetryCount = MaxBatchRetries);
        var retryable = await InsertBatch(run.Id, BatchStatus.Failed, b => b.RetryCount = MaxBatchRetries - 1);

        var swept = await repository.MarkBatchesPermanentlyFailed(run.Id, MaxBatchRetries);

        swept.Select(batch => batch.BatchId).Should().BeEquivalentTo([failedWithNoRetriesLeft.Id, abandonedWithNoRetriesLeft.Id]);
        foreach (var batchId in swept.Select(batch => batch.BatchId))
        {
            var stored = await StoredBatch(batchId);
            stored.Status.Should().Be(BatchStatus.PermanentlyFailed);
            stored.CompletedUtc.Should().BeCloseTo(DateTime.UtcNow, ClockTolerance);
        }

        (await StoredBatch(retryable.Id)).Status.Should().Be(BatchStatus.Failed);
    }

    [Test]
    public async Task RequeueRetryableBatches_SendsTheRetryableBatchesBackToPending_WithOneMoreRetryAndNoLease()
    {
        var run = await InsertRun();
        var failed = await InsertBatch(run.Id, BatchStatus.Failed, b =>
        {
            b.LeaseOwner = fixture.Create<Guid>();
            b.LeaseExpiresUtc = DateTime.UtcNow.AddHours(1);
        });
        var abandoned = await InsertBatch(run.Id, BatchStatus.Abandoned, b =>
        {
            b.RetryCount = 1;
            b.LeaseOwner = fixture.Create<Guid>();
        });
        var noRetriesLeft = await InsertBatch(run.Id, BatchStatus.Abandoned, b => b.RetryCount = MaxBatchRetries);

        var swept = await repository.RequeueRetryableBatches(run.Id, MaxBatchRetries);

        swept.Select(batch => batch.BatchId).Should().BeEquivalentTo([failed.Id, abandoned.Id]);
        foreach (var requeued in new[] { failed, abandoned })
        {
            var stored = await StoredBatch(requeued.Id);
            stored.Status.Should().Be(BatchStatus.Pending);
            stored.RetryCount.Should().Be(requeued.RetryCount + 1);
            stored.LeaseOwner.Should().BeNull();
            stored.LeaseExpiresUtc.Should().BeNull();
        }

        (await StoredBatch(noRetriesLeft.Id)).Status.Should().Be(BatchStatus.Abandoned);
    }

    [Test]
    public async Task TheTwoFailureSweeps_TogetherLeaveNoFailedOrAbandonedBatch()
    {
        // A failed or abandoned batch that neither sweep moves would never become terminal, and the stage would wait for
        // it forever.
        var run = await InsertRun();
        foreach (var status in new[] { BatchStatus.Failed, BatchStatus.Abandoned })
        foreach (var retryCount in new[] { 0, MaxBatchRetries - 1, MaxBatchRetries, MaxBatchRetries + 1 })
        {
            await InsertBatch(run.Id, status, b => b.RetryCount = retryCount);
        }

        await repository.MarkBatchesPermanentlyFailed(run.Id, MaxBatchRetries);
        await repository.RequeueRetryableBatches(run.Id, MaxBatchRetries);

        (await StoredBatches()).Select(batch => batch.Status).Should().OnlyContain(status =>
            status == BatchStatus.Pending || status == BatchStatus.PermanentlyFailed);
    }

    [TestCase(BatchStatus.Pending)]
    [TestCase(BatchStatus.Requested)]
    [TestCase(BatchStatus.InProgress)]
    public async Task TryMarkBatchAbandoned_ForABatchThatWaitsForItsMessage_AbandonsItWithTheReason(BatchStatus status)
    {
        var run = await InsertRun();
        var batch = await InsertBatch(run.Id, status);
        var reason = fixture.Create<string>();

        var abandoned = await repository.TryMarkBatchAbandoned(run.DataRefreshRecordId, run.Id, batch.Id, reason);

        abandoned.Should().BeTrue();
        var stored = await StoredBatch(batch.Id);
        stored.Status.Should().Be(BatchStatus.Abandoned);
        stored.FailureMessage.Should().Be(reason);
    }

    [TestCase(BatchStatus.ResultsReceived)]
    [TestCase(BatchStatus.PermanentlyFailed)]
    [TestCase(BatchStatus.Failed)]
    [TestCase(BatchStatus.Abandoned)]
    public async Task TryMarkBatchAbandoned_ForABatchThatWaitsForNoMessage_ReturnsFalseAndLeavesTheBatch(BatchStatus status)
    {
        var run = await InsertRun();
        var batch = await InsertBatch(run.Id, status);

        var abandoned = await repository.TryMarkBatchAbandoned(run.DataRefreshRecordId, run.Id, batch.Id, fixture.Create<string>());

        abandoned.Should().BeFalse();
        (await StoredBatch(batch.Id)).Status.Should().Be(status);
    }

    [Test]
    public async Task TryMarkBatchAbandoned_WhenTheRunIsNotRunning_ReturnsFalse()
    {
        var run = await InsertRun(RunStatus.Cancelled);
        var batch = await InsertBatch(run.Id, BatchStatus.Requested);

        var abandoned = await repository.TryMarkBatchAbandoned(run.DataRefreshRecordId, run.Id, batch.Id, fixture.Create<string>());

        abandoned.Should().BeFalse();
    }

    private DonorGenotypePrecomputationBatchClaim NewClaim(DonorGenotypePrecomputationRun run, DonorGenotypePrecomputationBatch batch) =>
        new(run.DataRefreshRecordId, run.Id, batch.Id, fixture.Create<Guid>(), LeaseDuration, IsRedelivery: false);

    private async Task<DonorGenotypePrecomputationRun> InsertRun(
        RunStatus status = RunStatus.Running,
        TransientDatabase database = TransientDatabase.DatabaseA)
    {
        var run = new DonorGenotypePrecomputationRun
        {
            DataRefreshRecordId = fixture.Create<int>(),
            // The column holds 32 characters; a whole AutoFixture string is 36.
            HlaNomenclatureVersion = fixture.Create<string>()[..8],
            Status = status,
            GroupsPerBatch = fixture.Create<int>(),
            CreatedUtc = DateTime.UtcNow,
            StatusDateUtc = DateTime.UtcNow
        };

        await Insert(run, database);
        return run;
    }

    private async Task<DonorGenotypePrecomputationBatch> InsertBatch(
        int runId,
        BatchStatus status = BatchStatus.Pending,
        Action<DonorGenotypePrecomputationBatch> customise = null)
    {
        var batch = NewBatch(runId, status);
        customise?.Invoke(batch);

        await Insert(batch);
        return batch;
    }

    private async Task<IReadOnlyCollection<int>> InsertPendingBatches(int runId, int count)
    {
        var batches = Enumerable.Range(0, count).Select(_ => NewBatch(runId, BatchStatus.Pending)).ToList();

        await using var context = NewContext();
        context.DonorGenotypePrecomputationBatches.AddRange(batches);
        await context.SaveChangesAsync();

        return [..batches.Select(batch => batch.Id)];
    }

    private DonorGenotypePrecomputationBatch NewBatch(int runId, BatchStatus status)
    {
        var firstGroupId = fixture.Create<int>();
        var groupCount = fixture.Create<int>();

        return new DonorGenotypePrecomputationBatch
        {
            RunId = runId,
            BatchNumber = nextBatchNumber++,
            FirstGroupId = firstGroupId,
            LastGroupId = firstGroupId + groupCount - 1,
            GroupCount = groupCount,
            DonorAssignmentCount = fixture.Create<int>(),
            Status = status,
            StatusDateUtc = DateTime.UtcNow
        };
    }

    private async Task<DonorGenotypePrecomputationGroup> InsertGroup(
        int runId,
        int groupId,
        int representativeDonorId,
        int? subjectGenotypeSetValueId = null,
        string failureMessage = null)
    {
        var group = new DonorGenotypePrecomputationGroup
        {
            Id = groupId,
            RunId = runId,
            AllowedLociKey = fixture.Create<AllowedLociKey>(),
            RepresentativeDonorId = representativeDonorId,
            DonorCount = fixture.Create<int>(),
            SubjectGenotypeSetValueId = subjectGenotypeSetValueId,
            FailureMessage = failureMessage
        };

        await Insert(group);
        return group;
    }

    private async Task<IReadOnlyCollection<int>> InsertGroupDonors(int groupId, int donorCount)
    {
        var donorIds = fixture.CreateMany<int>(donorCount).ToList();

        await using var context = NewContext();
        context.DonorGenotypePrecomputationGroupDonors.AddRange(
            donorIds.Select(donorId => new DonorGenotypePrecomputationGroupDonor { GroupId = groupId, DonorId = donorId }));
        await context.SaveChangesAsync();

        return donorIds;
    }

    private async Task<Donor> InsertDonor()
    {
        var donor = new Donor
        {
            DonorId = fixture.Create<int>(),
            DonorType = fixture.Create<DonorType>(),
            IsAvailableForSearch = true,
            ExternalDonorCode = fixture.Create<string>(),
            RegistryCode = fixture.Create<string>(),
            EthnicityCode = fixture.Create<string>(),
            A_1 = fixture.Create<string>(),
            A_2 = fixture.Create<string>(),
            B_1 = fixture.Create<string>(),
            B_2 = fixture.Create<string>(),
            C_1 = fixture.Create<string>(),
            C_2 = fixture.Create<string>(),
            DPB1_1 = fixture.Create<string>(),
            DPB1_2 = fixture.Create<string>(),
            DQB1_1 = fixture.Create<string>(),
            DQB1_2 = fixture.Create<string>(),
            DRB1_1 = fixture.Create<string>(),
            DRB1_2 = fixture.Create<string>()
        };

        await Insert(donor);
        return donor;
    }

    private async Task<DonorGenotypePrecomputationBatch> StoredBatch(int batchId)
    {
        await using var context = NewContext();
        return await context.DonorGenotypePrecomputationBatches.AsNoTracking().SingleAsync(batch => batch.Id == batchId);
    }

    private async Task<List<DonorGenotypePrecomputationBatch>> StoredBatches()
    {
        await using var context = NewContext();
        return await context.DonorGenotypePrecomputationBatches.AsNoTracking().ToListAsync();
    }

    private async Task<Dictionary<int, DonorGenotypePrecomputationGroup>> StoredGroups()
    {
        await using var context = NewContext();
        return await context.DonorGenotypePrecomputationGroups.AsNoTracking().ToDictionaryAsync(group => group.Id);
    }

    /// <summary>A fresh context for each write, so that change tracking cannot hide what the database holds.</summary>
    private async Task Insert<T>(T entity, TransientDatabase database = TransientDatabase.DatabaseA) where T : class
    {
        await using var context = NewContext(database);
        context.Set<T>().Add(entity);
        await context.SaveChangesAsync();
    }

    private SearchAlgorithmContext NewContext(TransientDatabase database = TransientDatabase.DatabaseA) =>
        new ContextFactory().Create(connectionStringFactory.GenerateConnectionStringProvider(database).GetConnectionString());
}
