using System;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Repositories;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.DataRefresh
{
    /// <summary>
    /// Covers which refresh records the watchdog treats as stalled, and so re-requests.
    /// These run against a real database because what is being tested is the predicate the query emits - in particular
    /// the COALESCE over two nullable columns - rather than any C# around it.
    /// </summary>
    [TestFixture]
    internal class DataRefreshStalledRecordTests
    {
        private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan GraceDuration = TimeSpan.FromMinutes(60);

        /// <summary>
        /// Records idle since before this count as stalled. Longer ago than <see cref="LeaseDuration"/>, as the
        /// watchdog requires, so the two windows can be told apart.
        /// </summary>
        private static DateTime GraceCutoff => DateTime.UtcNow - GraceDuration;

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

        [Test]
        public async Task GetStalledRefreshRecordIds_WhenLeaseIsLive_DoesNotReturnRecord()
        {
            // A refresh runs for many hours, so an old idle timestamp is normal for a healthy run. The live lease is
            // the only thing that distinguishes it from an abandoned one.
            var recordId = await AnOpenRecordLastActive(TimeSpan.FromHours(3));
            await dataRefreshHistoryRepository.TryClaimRefreshLease(recordId, Guid.NewGuid(), DateTime.UtcNow, LeaseDuration);

            var stalled = await dataRefreshHistoryRepository.GetStalledRefreshRecordIds(GraceCutoff);

            stalled.Should().NotContain(recordId);
        }

        [Test]
        public async Task GetStalledRefreshRecordIds_WhenLeaseExpiredWithinTheGracePeriod_DoesNotReturnRecord()
        {
            // Claimed 35 minutes ago under a 30 minute lease, so it lapsed 5 minutes ago - still inside the grace.
            var recordId = await AnOpenRecordLastActive(TimeSpan.FromHours(3));
            await dataRefreshHistoryRepository.TryClaimRefreshLease(
                recordId, Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-35), LeaseDuration);

            var stalled = await dataRefreshHistoryRepository.GetStalledRefreshRecordIds(GraceCutoff);

            stalled.Should().NotContain(recordId);
        }

        [Test]
        public async Task GetStalledRefreshRecordIds_WhenLeaseExpiredBeforeTheGracePeriod_ReturnsRecord()
        {
            var recordId = await AnOpenRecordLastActive(TimeSpan.FromHours(3));
            await dataRefreshHistoryRepository.TryClaimRefreshLease(
                recordId, Guid.NewGuid(), DateTime.UtcNow.AddHours(-3), LeaseDuration);

            var stalled = await dataRefreshHistoryRepository.GetStalledRefreshRecordIds(GraceCutoff);

            stalled.Should().Contain(recordId);
        }

        [Test]
        public async Task GetStalledRefreshRecordIds_WhenLeaseWasReleasedButTheRecordIsStillIdle_ReturnsRecord()
        {
            // A run that failed and released its lease on the way out, whose redelivery then never arrived.
            var recordId = await AnOpenRecordLastActive(TimeSpan.FromHours(3));
            var owner = Guid.NewGuid();
            await dataRefreshHistoryRepository.TryClaimRefreshLease(recordId, owner, DateTime.UtcNow.AddHours(-3), LeaseDuration);
            await dataRefreshHistoryRepository.ReleaseRefreshLease(recordId, owner);

            var stalled = await dataRefreshHistoryRepository.GetStalledRefreshRecordIds(GraceCutoff);

            stalled.Should().Contain(recordId);
        }

        [Test]
        public async Task GetStalledRefreshRecordIds_WhenARunWasAbandonedBeyondTheGracePeriod_ReturnsRecord()
        {
            // A run that started, stamped RefreshLastContinuedUtc, and was then abandoned without its lease ever
            // being recorded - a host killed between claiming and its first renewal write.
            var recordId = await AnOpenRecordLastActive(TimeSpan.FromHours(3));

            var stalled = await dataRefreshHistoryRepository.GetStalledRefreshRecordIds(GraceCutoff);

            stalled.Should().Contain(recordId);
        }

        [Test]
        public async Task GetStalledRefreshRecordIds_WhenRequestedButNeverRunBeyondTheGracePeriod_ReturnsRecord()
        {
            // The one stall no lease can reveal: a request message that was never delivered, so the record has never
            // been leased, never will be, and never had RefreshLastContinuedUtc stamped. Only the fall back to
            // RefreshRequestedUtc can age it, which makes this the test that holds that fall back in place.
            var recordId = await ARecordRequestedButNeverRun(TimeSpan.FromHours(3));

            var stalled = await dataRefreshHistoryRepository.GetStalledRefreshRecordIds(GraceCutoff);

            stalled.Should().Contain(recordId);
        }

        [Test]
        public async Task GetStalledRefreshRecordIds_WhenLeaseOwnerIsSetButTheExpiryIsMissing_DoesNotReturnRecord()
        {
            // TryClaimRefreshLease reads this as still held - the expiry comparison is UNKNOWN in SQL - so no
            // invocation could ever claim it. Returning it here would re-request it on every sweep, forever.
            var recordId = await dataRefreshHistoryRepository.Create(DataRefreshRecordBuilder.New
                .With(r => r.RefreshRequestedUtc, DateTime.UtcNow.AddHours(-3))
                .With(r => r.RefreshLastContinuedUtc, DateTime.UtcNow.AddHours(-3))
                .With(r => r.LeaseOwner, Guid.NewGuid())
                .Build());

            var stalled = await dataRefreshHistoryRepository.GetStalledRefreshRecordIds(GraceCutoff);

            stalled.Should().NotContain(recordId);
        }

        [Test]
        public async Task GetStalledRefreshRecordIds_WhenNeverLeasedButRequestedWithinTheGracePeriod_DoesNotReturnRecord()
        {
            // Just requested, with its message still in flight. Treating an absent lease as proof of a stall would
            // re-request this one, and make an auto-recovery indistinguishable from an ordinary request.
            var recordId = await ARecordRequestedButNeverRun(TimeSpan.FromSeconds(1));

            var stalled = await dataRefreshHistoryRepository.GetStalledRefreshRecordIds(GraceCutoff);

            stalled.Should().NotContain(recordId);
        }

        [Test]
        public async Task GetStalledRefreshRecordIds_WhenRequestedLongAgoButContinuedRecently_DoesNotReturnRecord()
        {
            // The continuation time must win over the request time: a record picked up a minute ago is active, however
            // long ago it was first requested.
            var recordId = await dataRefreshHistoryRepository.Create(DataRefreshRecordBuilder.New
                .With(r => r.RefreshRequestedUtc, DateTime.UtcNow.AddDays(-2))
                .With(r => r.RefreshLastContinuedUtc, DateTime.UtcNow.AddMinutes(-1))
                .Build());

            var stalled = await dataRefreshHistoryRepository.GetStalledRefreshRecordIds(GraceCutoff);

            stalled.Should().NotContain(recordId);
        }

        [Test]
        public async Task GetStalledRefreshRecordIds_WhenRecordIsCompleted_DoesNotReturnRecord()
        {
            // Long idle, and left holding a lapsed lease, but finished - so there is nothing to recover. The lease is
            // set directly here because a completed record cannot be claimed through the repository.
            var recordId = await dataRefreshHistoryRepository.Create(DataRefreshRecordBuilder.New
                .With(r => r.RefreshRequestedUtc, DateTime.UtcNow.AddHours(-3))
                .With(r => r.RefreshLastContinuedUtc, DateTime.UtcNow.AddHours(-3))
                .With(r => r.LeaseOwner, Guid.NewGuid())
                .With(r => r.LeaseExpiresUtc, DateTime.UtcNow.AddHours(-2))
                .SuccessfullyCompleted()
                .Build());

            var stalled = await dataRefreshHistoryRepository.GetStalledRefreshRecordIds(GraceCutoff);

            stalled.Should().NotContain(recordId);
        }

        /// <summary>
        /// An open record requested <paramref name="ago"/> in the past that was never picked up, so it carries no
        /// RefreshLastContinuedUtc. This is the shape a request message that was never delivered leaves behind.
        /// </summary>
        private async Task<int> ARecordRequestedButNeverRun(TimeSpan ago)
        {
            return await dataRefreshHistoryRepository.Create(DataRefreshRecordBuilder.New
                .With(r => r.RefreshRequestedUtc, DateTime.UtcNow - ago)
                .Build());
        }

        /// <summary>
        /// An open record that ran, and whose last sign of life was <paramref name="ago"/> in the past.
        /// </summary>
        private async Task<int> AnOpenRecordLastActive(TimeSpan ago)
        {
            var lastActive = DateTime.UtcNow - ago;
            return await dataRefreshHistoryRepository.Create(DataRefreshRecordBuilder.New
                .With(r => r.RefreshRequestedUtc, lastActive)
                .With(r => r.RefreshLastContinuedUtc, lastActive)
                .Build());
        }
    }
}
