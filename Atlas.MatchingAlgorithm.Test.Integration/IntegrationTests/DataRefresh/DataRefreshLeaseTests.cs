using System;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Repositories;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.DataRefresh
{
    /// <summary>
    /// Covers the run-level lease that keeps two invocations from processing the same refresh record at once.
    /// These run against a real database because the guarantee being tested is the atomicity of a single conditional
    /// UPDATE - which is a property of the SQL these methods emit, not of the C# around it.
    /// </summary>
    [TestFixture]
    internal class DataRefreshLeaseTests
    {
        private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(30);

        private ITestDataRefreshHistoryRepository dataRefreshHistoryRepository;

        [SetUp]
        public void SetUp()
        {
            dataRefreshHistoryRepository = DependencyInjection.DependencyInjection.Provider.GetService<ITestDataRefreshHistoryRepository>();
        }

        [TearDown]
        public async Task TearDown()
        {
            await dataRefreshHistoryRepository.RemoveAllDataRefreshRecords();
            IntegrationTestSetUp.RunInitialDataRefresh();
        }

        #region Claiming

        [Test]
        public async Task TryClaimRefreshLease_WhenRecordIsUnleased_ClaimsIt()
        {
            var recordId = await AnOpenRefreshRecord();
            var owner = Guid.NewGuid();
            var now = DateTime.UtcNow;

            var claimed = await dataRefreshHistoryRepository.TryClaimRefreshLease(recordId, owner, now, LeaseDuration);

            claimed.Should().BeTrue();
            var record = await ReadRecord(recordId);
            record.LeaseOwner.Should().Be(owner);
            record.LeaseExpiresUtc.Should().BeCloseTo(now + LeaseDuration, TimeSpan.FromSeconds(1));
        }

        [Test]
        public async Task TryClaimRefreshLease_WhenAnotherInvocationHoldsALiveLease_DoesNotClaimIt()
        {
            var recordId = await AnOpenRefreshRecord();
            var incumbent = Guid.NewGuid();
            await dataRefreshHistoryRepository.TryClaimRefreshLease(recordId, incumbent, DateTime.UtcNow, LeaseDuration);

            var claimed = await dataRefreshHistoryRepository.TryClaimRefreshLease(recordId, Guid.NewGuid(), DateTime.UtcNow, LeaseDuration);

            claimed.Should().BeFalse();
            (await ReadRecord(recordId)).LeaseOwner.Should().Be(incumbent);
        }

        [Test]
        public async Task TryClaimRefreshLease_WhenTheSameInvocationAlreadyHoldsIt_ClaimsItAgain()
        {
            // Makes redelivery to the invocation that is already running harmless.
            var recordId = await AnOpenRefreshRecord();
            var owner = Guid.NewGuid();
            await dataRefreshHistoryRepository.TryClaimRefreshLease(recordId, owner, DateTime.UtcNow, LeaseDuration);

            var reclaimed = await dataRefreshHistoryRepository.TryClaimRefreshLease(recordId, owner, DateTime.UtcNow, LeaseDuration);

            reclaimed.Should().BeTrue();
        }

        [Test]
        public async Task TryClaimRefreshLease_WhenTheIncumbentLeaseHasExpired_ClaimsIt()
        {
            // An owner that died without releasing must not block the next run indefinitely.
            var recordId = await AnOpenRefreshRecord();
            var deadOwner = Guid.NewGuid();
            await dataRefreshHistoryRepository.TryClaimRefreshLease(recordId, deadOwner, DateTime.UtcNow.AddHours(-2), LeaseDuration);
            var newOwner = Guid.NewGuid();

            var claimed = await dataRefreshHistoryRepository.TryClaimRefreshLease(recordId, newOwner, DateTime.UtcNow, LeaseDuration);

            claimed.Should().BeTrue();
            (await ReadRecord(recordId)).LeaseOwner.Should().Be(newOwner);
        }

        [Test]
        public async Task TryClaimRefreshLease_WhenRecordIsAlreadyCompleted_DoesNotClaimIt()
        {
            // This is what turns a request redelivered after completion into a no-op, rather than a dead-lettered
            // message that triggers a cleanup against whatever refresh is running by then.
            var recordId = await dataRefreshHistoryRepository.Create(DataRefreshRecordBuilder.New.SuccessfullyCompleted().Build());

            var claimed = await dataRefreshHistoryRepository.TryClaimRefreshLease(recordId, Guid.NewGuid(), DateTime.UtcNow, LeaseDuration);

            claimed.Should().BeFalse();
            (await ReadRecord(recordId)).LeaseOwner.Should().BeNull();
        }

        #endregion

        #region Renewing

        [Test]
        public async Task TryRenewRefreshLease_WhenHeldByTheRenewingInvocation_ExtendsTheExpiry()
        {
            var recordId = await AnOpenRefreshRecord();
            var owner = Guid.NewGuid();
            await dataRefreshHistoryRepository.TryClaimRefreshLease(recordId, owner, DateTime.UtcNow, LeaseDuration);
            var originalExpiry = (await ReadRecord(recordId)).LeaseExpiresUtc;

            var renewed = await dataRefreshHistoryRepository.TryRenewRefreshLease(recordId, owner, DateTime.UtcNow.AddMinutes(5), LeaseDuration);

            renewed.Should().BeTrue();
            (await ReadRecord(recordId)).LeaseExpiresUtc.Should().BeAfter(originalExpiry.Value);
        }

        [Test]
        public async Task TryRenewRefreshLease_WhenTakenOverByAnotherInvocation_Fails()
        {
            var recordId = await AnOpenRefreshRecord();
            var displacedOwner = Guid.NewGuid();
            await dataRefreshHistoryRepository.TryClaimRefreshLease(recordId, displacedOwner, DateTime.UtcNow.AddHours(-2), LeaseDuration);
            await dataRefreshHistoryRepository.TryClaimRefreshLease(recordId, Guid.NewGuid(), DateTime.UtcNow, LeaseDuration);

            var renewed = await dataRefreshHistoryRepository.TryRenewRefreshLease(recordId, displacedOwner, DateTime.UtcNow, LeaseDuration);

            renewed.Should().BeFalse();
        }

        #endregion

        #region Releasing

        [Test]
        public async Task ReleaseRefreshLease_WhenHeldByTheReleasingInvocation_ClearsTheLease()
        {
            var recordId = await AnOpenRefreshRecord();
            var owner = Guid.NewGuid();
            await dataRefreshHistoryRepository.TryClaimRefreshLease(recordId, owner, DateTime.UtcNow, LeaseDuration);

            var released = await dataRefreshHistoryRepository.ReleaseRefreshLease(recordId, owner);

            released.Should().BeTrue();
            var record = await ReadRecord(recordId);
            record.LeaseOwner.Should().BeNull();
            record.LeaseExpiresUtc.Should().BeNull();
        }

        [Test]
        public async Task ReleaseRefreshLease_WhenAlreadyTakenOver_LeavesTheSuccessorsLeaseIntact()
        {
            // A displaced invocation finishing its teardown must not hand the record away from the invocation that
            // legitimately owns it now.
            var recordId = await AnOpenRefreshRecord();
            var displacedOwner = Guid.NewGuid();
            await dataRefreshHistoryRepository.TryClaimRefreshLease(recordId, displacedOwner, DateTime.UtcNow.AddHours(-2), LeaseDuration);
            var successor = Guid.NewGuid();
            await dataRefreshHistoryRepository.TryClaimRefreshLease(recordId, successor, DateTime.UtcNow, LeaseDuration);

            var released = await dataRefreshHistoryRepository.ReleaseRefreshLease(recordId, displacedOwner);

            released.Should().BeFalse();
            (await ReadRecord(recordId)).LeaseOwner.Should().Be(successor);
        }

        #endregion

        [Test]
        public async Task MarkStageAsComplete_OnARecordTrackedSinceBeforeARenewal_DoesNotRevertTheRenewedExpiry()
        {
            // DataRefreshRunner fetches the refresh record once and keeps it change-tracked for the whole ~21 hour run,
            // while the heartbeat renews the lease from a separate scope. If the runner's SaveChanges wrote back the
            // whole entity, every stage completion would silently roll the lease expiry back to the value read at the
            // start of the run.
            var recordId = await AnOpenRefreshRecord();
            var owner = Guid.NewGuid();
            await dataRefreshHistoryRepository.TryClaimRefreshLease(recordId, owner, DateTime.UtcNow, LeaseDuration);

            using var runScope = DependencyInjection.DependencyInjection.BackingProvider.CreateScope();
            var runRepository = runScope.ServiceProvider.GetRequiredService<IDataRefreshHistoryRepository>();
            var trackedRecord = await runRepository.GetRecord(recordId);

            using var heartbeatScope = DependencyInjection.DependencyInjection.BackingProvider.CreateScope();
            var heartbeatRepository = heartbeatScope.ServiceProvider.GetRequiredService<IDataRefreshHistoryRepository>();
            var renewalTime = DateTime.UtcNow.AddMinutes(10);
            await heartbeatRepository.TryRenewRefreshLease(recordId, owner, renewalTime, LeaseDuration);

            await runRepository.MarkStageAsComplete(trackedRecord, DataRefreshStage.DonorImport);

            using var assertionScope = DependencyInjection.DependencyInjection.BackingProvider.CreateScope();
            var persisted = await assertionScope.ServiceProvider.GetRequiredService<IDataRefreshHistoryRepository>().GetRecord(recordId);
            persisted.LeaseExpiresUtc.Should().BeCloseTo(renewalTime + LeaseDuration, TimeSpan.FromSeconds(1));
            persisted.LeaseOwner.Should().Be(owner);
            persisted.DonorImportCompleted.Should().NotBeNull();
        }

        /// <summary>
        /// Reads the record on a scope of its own.
        /// </summary>
        /// <remarks>
        /// The lease methods write with ExecuteUpdateAsync, which bypasses the change tracker by design. A context that
        /// already has the record tracked - as the acting context does, having just created it - would therefore read
        /// back the values it last materialised rather than what is actually stored. Reading on a fresh scope asserts
        /// against the database, which is the thing under test.
        /// </remarks>
        private static async Task<DataRefreshRecord> ReadRecord(int recordId)
        {
            using var scope = DependencyInjection.DependencyInjection.BackingProvider.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<IDataRefreshHistoryRepository>().GetRecord(recordId);
        }

        private async Task<int> AnOpenRefreshRecord()
        {
            return await dataRefreshHistoryRepository.Create(DataRefreshRecordBuilder.New.Build());
        }
    }
}
