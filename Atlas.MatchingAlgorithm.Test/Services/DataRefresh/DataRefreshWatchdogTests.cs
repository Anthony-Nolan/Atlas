using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Client.Models.DataRefresh;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Exceptions;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Notifications;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using AutoFixture.Dsl;
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh
{
    [TestFixture]
    public class DataRefreshWatchdogTests
    {
        private const int LeaseDurationMinutes = 30;
        private const int GraceDurationMinutes = 60;
        private const int StalledRecordId = 123;

        private IDataRefreshHistoryRepository dataRefreshHistoryRepository;
        private IDataRefreshServiceBusClient serviceBusClient;
        private IMatchingAlgorithmImportLogger logger;

        private IDataRefreshWatchdog dataRefreshWatchdog;

        [SetUp]
        public void SetUp()
        {
            dataRefreshHistoryRepository = Substitute.For<IDataRefreshHistoryRepository>();
            serviceBusClient = Substitute.For<IDataRefreshServiceBusClient>();
            logger = Substitute.For<IMatchingAlgorithmImportLogger>();

            dataRefreshHistoryRepository.GetStalledRefreshRecordIds(default).ReturnsForAnyArgs(new List<int>());

            dataRefreshWatchdog = BuildDataRefreshWatchdog();
        }

        private IDataRefreshWatchdog BuildDataRefreshWatchdog(DataRefreshSettings dataRefreshSettings = null)
        {
            return new DataRefreshWatchdog(
                dataRefreshHistoryRepository,
                serviceBusClient,
                dataRefreshSettings ?? DefaultSettings().Build(),
                logger
            );
        }

        private static IPostprocessComposer<DataRefreshSettings> DefaultSettings() =>
            DataRefreshSettingsBuilder.New
                .With(s => s.LeaseDurationMinutes, LeaseDurationMinutes)
                .With(s => s.WatchdogGraceDurationMinutes, GraceDurationMinutes);

        private void GivenStalledRecords(params int[] recordIds)
        {
            dataRefreshHistoryRepository.GetStalledRefreshRecordIds(default).ReturnsForAnyArgs(recordIds);
        }

        [Test]
        public async Task RecoverStalledRefreshes_ReRequestsEachStalledRecord()
        {
            GivenStalledRecords(StalledRecordId);

            await dataRefreshWatchdog.RecoverStalledRefreshes();

            await serviceBusClient.Received(1).PublishToRequestTopic(
                Arg.Is<ValidatedDataRefreshRequest>(r => r.DataRefreshRecordId == StalledRecordId));
        }

        [Test]
        public async Task RecoverStalledRefreshes_WithMultipleStalledRecords_ReRequestsAllOfThem()
        {
            GivenStalledRecords(1, 2, 3);

            await dataRefreshWatchdog.RecoverStalledRefreshes();

            await serviceBusClient.Received(3).PublishToRequestTopic(Arg.Any<ValidatedDataRefreshRequest>());
            foreach (var recordId in new[] {1, 2, 3})
            {
                await serviceBusClient.Received(1).PublishToRequestTopic(
                    Arg.Is<ValidatedDataRefreshRequest>(r => r.DataRefreshRecordId == recordId));
            }
        }

        [Test]
        public async Task RecoverStalledRefreshes_WhenNothingHasStalled_PublishesNothing()
        {
            GivenStalledRecords();

            await dataRefreshWatchdog.RecoverStalledRefreshes();

            await serviceBusClient.DidNotReceiveWithAnyArgs().PublishToRequestTopic(default);
        }

        [Test]
        public async Task RecoverStalledRefreshes_WhenNothingHasStalled_LogsNothing()
        {
            // The overwhelmingly common case. A sweep that announced itself every few minutes would bury the sweeps
            // that actually found something.
            GivenStalledRecords();

            await dataRefreshWatchdog.RecoverStalledRefreshes();

            logger.DidNotReceiveWithAnyArgs().SendTrace(default, default, default);
            logger.DidNotReceiveWithAnyArgs().SendEvent(default, default, default, default);
        }

        [Test]
        public async Task RecoverStalledRefreshes_EmitsAnEventPerRecoveredRecord()
        {
            // Distinguishes a stall that healed itself from a refresh that was requested normally.
            GivenStalledRecords(1, 2);

            await dataRefreshWatchdog.RecoverStalledRefreshes();

            logger.Received(2).SendEvent(
                Arg.Any<string>(), Arg.Any<LogLevel>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<Dictionary<string, double>>());
        }

        [Test]
        public async Task RecoverStalledRefreshes_EventIdentifiesTheRecoveredRecord()
        {
            GivenStalledRecords(StalledRecordId);

            await dataRefreshWatchdog.RecoverStalledRefreshes();

            logger.Received(1).SendEvent(
                Arg.Any<string>(),
                Arg.Any<LogLevel>(),
                Arg.Is<Dictionary<string, string>>(p => p["DataRefreshRecordId"] == StalledRecordId.ToString()),
                Arg.Any<Dictionary<string, double>>());
        }

        [Test]
        public async Task RecoverStalledRefreshes_WhenPublishingFails_DoesNotEmitARecoveryEventForThatRecord()
        {
            // The event means "a request is on the topic". Emitting it for a publish that threw would report a recovery
            // that did not happen.
            GivenStalledRecords(StalledRecordId);
            serviceBusClient.PublishToRequestTopic(Arg.Any<ValidatedDataRefreshRequest>()).ThrowsAsync(new Exception("bus is down"));

            await dataRefreshWatchdog.Invoking(w => w.RecoverStalledRefreshes()).Should().ThrowAsync<AggregateException>();

            logger.DidNotReceiveWithAnyArgs().SendEvent(default, default, default, default);
        }

        [Test]
        public async Task RecoverStalledRefreshes_WhenOneRecordFails_StillReRequestsTheOthers()
        {
            GivenStalledRecords(1, 2, 3);
            serviceBusClient.PublishToRequestTopic(Arg.Is<ValidatedDataRefreshRequest>(r => r.DataRefreshRecordId == 2))
                .ThrowsAsync(new Exception("bus is down"));

            await dataRefreshWatchdog.Invoking(w => w.RecoverStalledRefreshes()).Should().ThrowAsync<AggregateException>();

            await serviceBusClient.Received(1).PublishToRequestTopic(Arg.Is<ValidatedDataRefreshRequest>(r => r.DataRefreshRecordId == 1));
            await serviceBusClient.Received(1).PublishToRequestTopic(Arg.Is<ValidatedDataRefreshRequest>(r => r.DataRefreshRecordId == 3));
        }

        [Test]
        public async Task RecoverStalledRefreshes_WhenOneRecordFails_ThrowsOnlyOnceTheSweepIsComplete()
        {
            GivenStalledRecords(1, 2);
            serviceBusClient.PublishToRequestTopic(Arg.Is<ValidatedDataRefreshRequest>(r => r.DataRefreshRecordId == 1))
                .ThrowsAsync(new Exception("bus is down"));

            var thrown = await dataRefreshWatchdog.Invoking(w => w.RecoverStalledRefreshes()).Should().ThrowAsync<AggregateException>();

            thrown.Which.InnerExceptions.Should().HaveCount(1);
        }

        [Test]
        public async Task RecoverStalledRefreshes_WhenEveryRecordSucceeds_DoesNotThrow()
        {
            GivenStalledRecords(1, 2, 3);

            await dataRefreshWatchdog.Invoking(w => w.RecoverStalledRefreshes()).Should().NotThrowAsync();
        }

        [Test]
        public async Task RecoverStalledRefreshes_LooksBackByTheConfiguredGracePeriod()
        {
            var expectedCutoff = DateTime.UtcNow.AddMinutes(-GraceDurationMinutes);

            await dataRefreshWatchdog.RecoverStalledRefreshes();

            await dataRefreshHistoryRepository.Received(1).GetStalledRefreshRecordIds(
                Arg.Is<DateTime>(cutoff => cutoff > expectedCutoff.AddSeconds(-30) && cutoff < expectedCutoff.AddSeconds(30)));
        }

        [Test]
        public async Task RecoverStalledRefreshes_WhenGraceIsShorterThanTheLease_ThrowsWithoutQuerying()
        {
            // A grace within the lease would let clock skew between the refreshing host and this one decide whether a
            // live run is re-requested.
            var settings = DefaultSettings().With(s => s.WatchdogGraceDurationMinutes, LeaseDurationMinutes - 1).Build();

            await BuildDataRefreshWatchdog(settings).Invoking(w => w.RecoverStalledRefreshes())
                .Should().ThrowAsync<InvalidDataRefreshConfigurationException>();

            await dataRefreshHistoryRepository.DidNotReceiveWithAnyArgs().GetStalledRefreshRecordIds(default);
        }

        [Test]
        public async Task RecoverStalledRefreshes_WhenGraceEqualsTheLease_Throws()
        {
            var settings = DefaultSettings().With(s => s.WatchdogGraceDurationMinutes, LeaseDurationMinutes).Build();

            await BuildDataRefreshWatchdog(settings).Invoking(w => w.RecoverStalledRefreshes())
                .Should().ThrowAsync<InvalidDataRefreshConfigurationException>();
        }

        [Test]
        public async Task RecoverStalledRefreshes_WhenGraceIsNotPositive_Throws()
        {
            var settings = DefaultSettings().With(s => s.WatchdogGraceDurationMinutes, 0).Build();

            await BuildDataRefreshWatchdog(settings).Invoking(w => w.RecoverStalledRefreshes())
                .Should().ThrowAsync<InvalidDataRefreshConfigurationException>();
        }

        [Test]
        public async Task RecoverStalledRefreshes_DoesNotClaimALeaseOfItsOwn()
        {
            // The orchestrator's claim is what decides who runs a record; a watchdog that claimed first would fence the
            // invocation it just asked to do the work.
            GivenStalledRecords(StalledRecordId);

            await dataRefreshWatchdog.RecoverStalledRefreshes();

            await dataRefreshHistoryRepository.DidNotReceiveWithAnyArgs().TryClaimRefreshLease(default, default, default, default);
        }
    }
}
