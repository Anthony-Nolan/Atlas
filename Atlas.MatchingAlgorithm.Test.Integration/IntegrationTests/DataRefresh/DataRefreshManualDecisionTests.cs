using System;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Repositories;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.DataRefresh;

/// <summary>
/// Covers the pause of a refresh record for a manual decision, and the decision itself. These run against a real database
/// because the guarantees under test are those of single conditional UPDATEs: the pause is fenced by the lease owner, and
/// a decision is a compare-and-swap that only one caller can win.
/// </summary>
[TestFixture]
internal class DataRefreshManualDecisionTests
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(30);

    private Fixture fixture;
    private ITestDataRefreshHistoryRepository dataRefreshHistoryRepository;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        dataRefreshHistoryRepository = DependencyInjection.DependencyInjection.Provider.GetService<ITestDataRefreshHistoryRepository>();
    }

    [TearDown]
    public async Task TearDown()
    {
        await dataRefreshHistoryRepository.RemoveAllDataRefreshRecords();
        IntegrationTestSetUp.RunInitialDataRefresh();
    }

    #region The pause

    [Test]
    public async Task TryPauseAndReleaseLease_ForTheLeaseOwner_SetsThePauseAndClearsTheLeaseInOneUpdate()
    {
        var recordId = await AnOpenRefreshRecord();
        var owner = Guid.NewGuid();
        await dataRefreshHistoryRepository.TryClaimRefreshLease(recordId, owner, DateTime.UtcNow, LeaseDuration);
        var pausedSince = DateTime.UtcNow;

        var wasPaused = await dataRefreshHistoryRepository.TryPauseAndReleaseLease(recordId, owner, pausedSince);

        wasPaused.Should().BeTrue();
        var record = await ReadRecord(recordId);
        record.AwaitingManualDecisionSinceUtc.Should().BeCloseTo(pausedSince, TimeSpan.FromMilliseconds(10));
        record.LeaseOwner.Should().BeNull();
        record.LeaseExpiresUtc.Should().BeNull();
        record.RefreshEndUtc.Should().BeNull();
    }

    [Test]
    public async Task TryPauseAndReleaseLease_ForAnotherOwner_ChangesNothing()
    {
        // An invocation that lost the record must not pause it under the invocation that holds it now.
        var recordId = await AnOpenRefreshRecord();
        var owner = Guid.NewGuid();
        await dataRefreshHistoryRepository.TryClaimRefreshLease(recordId, owner, DateTime.UtcNow, LeaseDuration);

        var wasPaused = await dataRefreshHistoryRepository.TryPauseAndReleaseLease(recordId, Guid.NewGuid(), DateTime.UtcNow);

        wasPaused.Should().BeFalse();
        var record = await ReadRecord(recordId);
        record.AwaitingManualDecisionSinceUtc.Should().BeNull();
        record.LeaseOwner.Should().Be(owner);
    }

    [Test]
    public async Task TryPauseAndReleaseLease_ForAClosedRecord_ChangesNothing()
    {
        var owner = Guid.NewGuid();
        var recordId = await dataRefreshHistoryRepository.Create(DataRefreshRecordBuilder.New
            .SuccessfullyCompleted()
            .With(r => r.LeaseOwner, owner)
            .With(r => r.LeaseExpiresUtc, DateTime.UtcNow.Add(LeaseDuration))
            .Build());

        var wasPaused = await dataRefreshHistoryRepository.TryPauseAndReleaseLease(recordId, owner, DateTime.UtcNow);

        wasPaused.Should().BeFalse();
        (await ReadRecord(recordId)).AwaitingManualDecisionSinceUtc.Should().BeNull();
    }

    #endregion

    #region The decision

    [TestCase(DataRefreshManualDecision.Complete)]
    [TestCase(DataRefreshManualDecision.RetryFailedBatches)]
    public async Task TryTakeManualDecision_CompleteOrRetry_EndsThePause_AndRecordsThePendingDecision(DataRefreshManualDecision decision)
    {
        var recordId = await APausedRecord();

        var wasTaken = await dataRefreshHistoryRepository.TryTakeManualDecision(recordId, decision, DateTime.UtcNow, fixture.Create<string>());

        wasTaken.Should().BeTrue();
        var record = await ReadRecord(recordId);
        record.AwaitingManualDecisionSinceUtc.Should().BeNull();
        record.PendingManualDecision.Should().Be(decision);
        record.RefreshEndUtc.Should().BeNull();
        record.WasSuccessful.Should().BeNull();
    }

    [Test]
    public async Task TryTakeManualDecision_Fail_EndsThePause_AndClosesTheRecordAsFailed()
    {
        var recordId = await APausedRecord();
        var now = DateTime.UtcNow;

        var wasTaken = await dataRefreshHistoryRepository.TryTakeManualDecision(recordId, DataRefreshManualDecision.Fail, now, fixture.Create<string>());

        wasTaken.Should().BeTrue();
        var record = await ReadRecord(recordId);
        record.AwaitingManualDecisionSinceUtc.Should().BeNull();
        record.PendingManualDecision.Should().BeNull();
        record.RefreshEndUtc.Should().BeCloseTo(now, TimeSpan.FromMilliseconds(10));
        record.WasSuccessful.Should().BeFalse();
    }

    [Test]
    public async Task TryTakeManualDecision_AppendsTheSupportCommentAsANewLine_AndKeepsTheExistingText()
    {
        var existingComments = fixture.Create<string>();
        var recordId = await dataRefreshHistoryRepository.Create(DataRefreshRecordBuilder.New
            .PausedForManualDecision()
            .With(r => r.SupportComments, existingComments)
            .Build());
        var comment = fixture.Create<string>();

        await dataRefreshHistoryRepository.TryTakeManualDecision(recordId, DataRefreshManualDecision.Complete, DateTime.UtcNow, comment);

        (await ReadRecord(recordId)).SupportComments.Should().Be(existingComments + Environment.NewLine + comment);
    }

    [Test]
    public async Task TryTakeManualDecision_WhenThereAreNoSupportComments_StoresOnlyTheComment()
    {
        var recordId = await APausedRecord();
        var comment = fixture.Create<string>();

        await dataRefreshHistoryRepository.TryTakeManualDecision(recordId, DataRefreshManualDecision.Complete, DateTime.UtcNow, comment);

        (await ReadRecord(recordId)).SupportComments.Should().Be(comment);
    }

    [Test]
    public async Task TryTakeManualDecision_ASecondDecision_ChangesNothing(
        [Values] DataRefreshManualDecision firstDecision,
        [Values] DataRefreshManualDecision secondDecision)
    {
        var recordId = await APausedRecord();
        await dataRefreshHistoryRepository.TryTakeManualDecision(recordId, firstDecision, DateTime.UtcNow, fixture.Create<string>());
        var afterTheFirstDecision = await ReadRecord(recordId);

        var wasTaken = await dataRefreshHistoryRepository.TryTakeManualDecision(
            recordId, secondDecision, DateTime.UtcNow, fixture.Create<string>());

        wasTaken.Should().BeFalse();
        (await ReadRecord(recordId)).Should().BeEquivalentTo(afterTheFirstDecision);
    }

    [Test]
    public async Task TryTakeManualDecision_ForARecordThatIsNotPaused_ChangesNothing([Values] DataRefreshManualDecision decision)
    {
        var recordId = await AnOpenRefreshRecord();
        var before = await ReadRecord(recordId);

        var wasTaken = await dataRefreshHistoryRepository.TryTakeManualDecision(recordId, decision, DateTime.UtcNow, fixture.Create<string>());

        wasTaken.Should().BeFalse();
        (await ReadRecord(recordId)).Should().BeEquivalentTo(before);
    }

    [Test]
    public async Task TryTakeManualDecision_ForAClosedRecord_ChangesNothing([Values] DataRefreshManualDecision decision)
    {
        var recordId = await dataRefreshHistoryRepository.Create(DataRefreshRecordBuilder.New
            .PausedForManualDecision()
            .SuccessfullyCompleted()
            .Build());
        var before = await ReadRecord(recordId);

        var wasTaken = await dataRefreshHistoryRepository.TryTakeManualDecision(recordId, decision, DateTime.UtcNow, fixture.Create<string>());

        wasTaken.Should().BeFalse();
        (await ReadRecord(recordId)).Should().BeEquivalentTo(before);
    }

    #endregion

    #region Applying and clearing a decision

    [Test]
    public async Task ApplyManualCompleteDecision_MarksThePrecomputationCompleteAndClearsTheDecision()
    {
        var recordId = await ARecordWithAPendingDecision(DataRefreshManualDecision.Complete);
        using var scope = DependencyInjection.DependencyInjection.BackingProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDataRefreshHistoryRepository>();
        var trackedRecord = await repository.GetRecord(recordId);

        await repository.ApplyManualCompleteDecision(trackedRecord);

        var record = await ReadRecord(recordId);
        record.DonorGenotypePrecomputationCompleted.Should().NotBeNull();
        record.PendingManualDecision.Should().BeNull();
    }

    [Test]
    public async Task ApplyManualCompleteDecision_OnARecordTrackedSinceBeforeARenewal_DoesNotRevertTheRenewedExpiry()
    {
        // The same shape as MarkStageAsComplete: the runner holds the record tracked while the heartbeat renews the lease
        // from another scope.
        var recordId = await ARecordWithAPendingDecision(DataRefreshManualDecision.Complete);
        var owner = Guid.NewGuid();
        await dataRefreshHistoryRepository.TryClaimRefreshLease(recordId, owner, DateTime.UtcNow, LeaseDuration);

        using var runScope = DependencyInjection.DependencyInjection.BackingProvider.CreateScope();
        var runRepository = runScope.ServiceProvider.GetRequiredService<IDataRefreshHistoryRepository>();
        var trackedRecord = await runRepository.GetRecord(recordId);

        using var heartbeatScope = DependencyInjection.DependencyInjection.BackingProvider.CreateScope();
        var renewalTime = DateTime.UtcNow.AddMinutes(10);
        await heartbeatScope.ServiceProvider.GetRequiredService<IDataRefreshHistoryRepository>()
            .TryRenewRefreshLease(recordId, owner, renewalTime + LeaseDuration);

        await runRepository.ApplyManualCompleteDecision(trackedRecord);

        var persisted = await ReadRecord(recordId);
        persisted.LeaseExpiresUtc.Should().BeCloseTo(renewalTime + LeaseDuration, TimeSpan.FromSeconds(1));
        persisted.LeaseOwner.Should().Be(owner);
        persisted.DonorGenotypePrecomputationCompleted.Should().NotBeNull();
    }

    [Test]
    public async Task ClearPendingManualDecision_ClearsOnlyTheDecision()
    {
        var recordId = await ARecordWithAPendingDecision(DataRefreshManualDecision.RetryFailedBatches);
        using var scope = DependencyInjection.DependencyInjection.BackingProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDataRefreshHistoryRepository>();
        var trackedRecord = await repository.GetRecord(recordId);

        await repository.ClearPendingManualDecision(trackedRecord);

        var record = await ReadRecord(recordId);
        record.PendingManualDecision.Should().BeNull();
        record.DonorGenotypePrecomputationCompleted.Should().BeNull();
    }

    [Test]
    public async Task ClearManualDecisionState_ClearsThePauseAndThePendingDecision()
    {
        var recordId = await dataRefreshHistoryRepository.Create(DataRefreshRecordBuilder.New
            .PausedForManualDecision()
            .WithPendingManualDecision(DataRefreshManualDecision.RetryFailedBatches)
            .Build());

        await dataRefreshHistoryRepository.ClearManualDecisionState(recordId);

        var record = await ReadRecord(recordId);
        record.AwaitingManualDecisionSinceUtc.Should().BeNull();
        record.PendingManualDecision.Should().BeNull();
    }

    #endregion

    #region Reading

    [Test]
    public async Task FindRecord_ForAnUnknownId_ReturnsNull()
    {
        var record = await dataRefreshHistoryRepository.FindRecord(-fixture.Create<int>());

        record.Should().BeNull();
    }

    [Test]
    public async Task FindRecord_AfterAnUpdateThatBypassesTheChangeTracker_ReturnsTheStoredValues()
    {
        // The decision endpoint reads the record again after an update that changed no row, to say why. A tracked read would
        // return the values that the context read before.
        var recordId = await APausedRecord();
        await dataRefreshHistoryRepository.FindRecord(recordId);
        await dataRefreshHistoryRepository.TryTakeManualDecision(
            recordId, DataRefreshManualDecision.Complete, DateTime.UtcNow, fixture.Create<string>());

        var record = await dataRefreshHistoryRepository.FindRecord(recordId);

        record.PendingManualDecision.Should().Be(DataRefreshManualDecision.Complete);
        record.AwaitingManualDecisionSinceUtc.Should().BeNull();
    }

    #endregion

    /// <summary>
    /// Reads the record on a scope of its own. ExecuteUpdateAsync bypasses the change tracker, so a context that tracks the
    /// record would return the values that it read before.
    /// </summary>
    private static async Task<DataRefreshRecord> ReadRecord(int recordId)
    {
        using var scope = DependencyInjection.DependencyInjection.BackingProvider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IDataRefreshHistoryRepository>().GetRecord(recordId);
    }

    private async Task<int> AnOpenRefreshRecord() => await dataRefreshHistoryRepository.Create(DataRefreshRecordBuilder.New.Build());

    /// <summary>
    /// A paused record with no <see cref="DataRefreshRecord.WasSuccessful"/> value, as an open record has: the requester
    /// does not set it. The builder sets false, which would hide whether a decision writes it.
    /// </summary>
    private async Task<int> APausedRecord() =>
        await dataRefreshHistoryRepository.Create(DataRefreshRecordBuilder.New
            .PausedForManualDecision()
            .With(r => r.WasSuccessful, (bool?)null)
            .Build());

    private async Task<int> ARecordWithAPendingDecision(DataRefreshManualDecision decision) =>
        await dataRefreshHistoryRepository.Create(DataRefreshRecordBuilder.New
            .WithStagesCompletedUpToAndIncluding(DataRefreshStage.IndexRecreation)
            .WithPendingManualDecision(decision)
            .Build());
}
