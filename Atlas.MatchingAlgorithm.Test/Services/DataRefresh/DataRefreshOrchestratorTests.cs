using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Utils.Http;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Notifications;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh
{
    [TestFixture]
    public class DataRefreshOrchestratorTests
    {
        private IMatchingAlgorithmImportLogger logger;
        private IActiveDatabaseProvider activeDatabaseProvider;
        private IDataRefreshRunner dataRefreshRunner;
        private IDataRefreshHistoryRepository dataRefreshHistoryRepository;

        private IAzureDatabaseManager azureDatabaseManager;
        private IDataRefreshSupportNotificationSender dataRefreshSupportNotificationSender;
        private IDataRefreshCompletionNotifier dataRefreshCompletionNotifier;
        private IDonorGenotypePrecomputationRunCanceller precomputationRunCanceller;

        private IServiceScopeFactory serviceScopeFactory;

        private Fixture fixture;

        private IDataRefreshOrchestrator dataRefreshOrchestrator;
        private const string ExistingHlaVersion = "old";
        private const string NewHlaVersion = "new";
        private const int DefaultRecordId = 123;

        [SetUp]
        public void SetUp()
        {
            fixture = new Fixture();

            logger = Substitute.For<IMatchingAlgorithmImportLogger>();
            activeDatabaseProvider = Substitute.For<IActiveDatabaseProvider>();
            dataRefreshRunner = Substitute.For<IDataRefreshRunner>();
            dataRefreshHistoryRepository = Substitute.For<IDataRefreshHistoryRepository>();
            azureDatabaseManager = Substitute.For<IAzureDatabaseManager>();
            dataRefreshSupportNotificationSender = Substitute.For<IDataRefreshSupportNotificationSender>();
            dataRefreshCompletionNotifier = Substitute.For<IDataRefreshCompletionNotifier>();
            precomputationRunCanceller = Substitute.For<IDonorGenotypePrecomputationRunCanceller>();
            serviceScopeFactory = BuildServiceScopeFactoryResolving(dataRefreshHistoryRepository);

            dataRefreshOrchestrator = BuildDataRefreshOrchestrator();

            var record = DataRefreshRecordBuilder.New
                .With(r => r.Id, DefaultRecordId)
                .Build();
            dataRefreshHistoryRepository.GetIncompleteRefreshJobs().Returns(new[] {record});
            dataRefreshHistoryRepository.GetRecord(Arg.Any<int>()).Returns(record);

            // The happy path: this invocation takes the lease, holds it for the duration of the run, and gives it back.
            dataRefreshHistoryRepository.TryClaimRefreshLease(default, default, default, default).ReturnsForAnyArgs(true);
            dataRefreshHistoryRepository.TryRenewRefreshLease(default, default, default).ReturnsForAnyArgs(true);
            dataRefreshHistoryRepository.ReleaseRefreshLease(default, default).ReturnsForAnyArgs(true);
        }

        /// <summary>
        /// The lease heartbeat resolves its own DI scope per renewal, so that it does not share a DbContext with the run
        /// it is protecting. Resolving the same substitute keeps assertions about renewal simple.
        /// </summary>
        private static IServiceScopeFactory BuildServiceScopeFactoryResolving(IDataRefreshHistoryRepository historyRepository)
        {
            var scopedProvider = Substitute.For<IServiceProvider>();
            scopedProvider.GetService(typeof(IDataRefreshHistoryRepository)).Returns(historyRepository);

            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(scopedProvider);

            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            scopeFactory.CreateScope().Returns(scope);
            return scopeFactory;
        }

        private DataRefreshOrchestrator BuildDataRefreshOrchestrator(DataRefreshSettings dataRefreshSettings = null)
        {
            var activeHlaVersionAccessor = Substitute.For<IActiveHlaNomenclatureVersionAccessor>();
            activeHlaVersionAccessor.DoesActiveHlaNomenclatureVersionExist().Returns(true);
            activeHlaVersionAccessor.GetActiveHlaNomenclatureVersion().Returns(ExistingHlaVersion);

            var settings = dataRefreshSettings ?? DataRefreshSettingsBuilder.New.Build();

            return new DataRefreshOrchestrator(
                logger,
                settings,
                activeDatabaseProvider,
                dataRefreshRunner,
                dataRefreshHistoryRepository,
                azureDatabaseManager,
                new AzureDatabaseNameProvider(settings),
                dataRefreshSupportNotificationSender,
                dataRefreshCompletionNotifier,
                precomputationRunCanceller,
                serviceScopeFactory
            );
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenNoIncompleteJobs_ThrowsException()
        {
            dataRefreshHistoryRepository.GetIncompleteRefreshJobs().Returns(new List<DataRefreshRecord>());

            await dataRefreshOrchestrator.Invoking(r => r.OrchestrateDataRefresh(0)).Should().ThrowAsync<AtlasHttpException>();
        }

        [Test]
        public async Task OrchestrateDataRefresh_WithMultipleIncompleteJobs_ThrowsException()
        {
            dataRefreshHistoryRepository.GetIncompleteRefreshJobs().Returns(DataRefreshRecordBuilder.New.Build(2));

            await dataRefreshOrchestrator.Invoking(r => r.OrchestrateDataRefresh(0)).Should().ThrowAsync<AtlasHttpException>();
        }

        [Test]
        public async Task OrchestrateDataRefresh_SendsNotification()
        {
            const int recordId = 20;
            const int currentAttemptNumber = 2;

            var record = DataRefreshRecordBuilder.New
                .With(r => r.Id, recordId)
                .With(r => r.RefreshAttemptedCount, currentAttemptNumber - 1)
                .Build();
            dataRefreshHistoryRepository.GetIncompleteRefreshJobs().Returns(new[] {record});

            await dataRefreshOrchestrator.OrchestrateDataRefresh(recordId);

            await dataRefreshSupportNotificationSender.ReceivedWithAnyArgs().SendInProgressNotification(recordId, currentAttemptNumber);
        }

        [Test]
        public async Task OrchestrateDataRefresh_UpdatesRunAttemptDetails()
        {
            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await dataRefreshHistoryRepository.Received().UpdateRunAttemptDetails(DefaultRecordId);
        }

        [Test]
        public async Task OrchestrateDataRefresh_TriggersRefresh()
        {
            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await dataRefreshRunner.Received().RefreshData(DefaultRecordId, Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task OrchestrateDataRefresh_EventuallyRecordsDataRefreshOccurredWithLatestWmdaVersion()
        {
            dataRefreshRunner.RefreshData(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(NewHlaVersion);

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);
            await dataRefreshHistoryRepository.Received().UpdateExecutionDetails(DefaultRecordId, NewHlaVersion, Arg.Any<DateTime?>());
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenJobSuccessful_StoresRecordAsSuccess()
        {
            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await dataRefreshHistoryRepository.Received().UpdateSuccessFlag(DefaultRecordId, true);
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenJobSuccessful_UpdatesExecutionDetails()
        {
            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await dataRefreshHistoryRepository.ReceivedWithAnyArgs().UpdateExecutionDetails(default, default, default);
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenDataRefreshFails_LogsExceptionDetails()
        {
            const string exceptionMessage = "something very bad happened";
            dataRefreshRunner.RefreshData(Arg.Any<int>(), Arg.Any<CancellationToken>()).Throws(new Exception(exceptionMessage));

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            logger.Received().SendTrace(Arg.Is<string>(e => e.Contains(exceptionMessage)), LogLevel.Critical);
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenDataRefreshFails_UpdatesExecutionDetails()
        {
            const string exceptionMessage = "something very bad happened";
            dataRefreshRunner.RefreshData(Arg.Any<int>(), Arg.Any<CancellationToken>()).Throws(new Exception(exceptionMessage));

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await dataRefreshHistoryRepository.ReceivedWithAnyArgs().UpdateExecutionDetails(default, default, default);
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenDataRefreshFails_StoresSuccessFlagAsFalse()
        {
            const string exceptionMessage = "something very bad happened";
            dataRefreshRunner.RefreshData(Arg.Any<int>(), Arg.Any<CancellationToken>()).Throws(new Exception(exceptionMessage));

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await dataRefreshHistoryRepository.Received().UpdateSuccessFlag(DefaultRecordId, false);
        }

        [Test]
        public async Task RefreshData_ScalesActiveDatabaseToDormantSize()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DatabaseAName, "db-a")
                .With(s => s.DormantDatabaseSize, "S0")
                .With(s => s.DormantDatabaseAutoPauseTimeout, 60)
                .Build();
            dataRefreshOrchestrator = BuildDataRefreshOrchestrator(settings);
            activeDatabaseProvider.GetActiveDatabase().Returns(TransientDatabase.DatabaseA);

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await azureDatabaseManager.Received()
                .UpdateDatabaseSize(settings.DatabaseAName, AzureDatabaseSize.S0, settings.DormantDatabaseAutoPauseTimeout);
        }

        [Test]
        public async Task RefreshData_ScalesDownDatabaseThatWasActiveWhenTheJobStarted()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DatabaseAName, "db-a")
                .With(s => s.DormantDatabaseSize, "S0")
                .Build();
            dataRefreshOrchestrator = BuildDataRefreshOrchestrator(settings);
            activeDatabaseProvider.GetActiveDatabase().Returns(TransientDatabase.DatabaseA);

            // Marking refresh record as complete will switch over which database is considered "active". Emulating this with mocks here.
            dataRefreshHistoryRepository.WhenForAnyArgs(r => r.UpdateSuccessFlag(0, true)).Do(x =>
            {
                activeDatabaseProvider.GetActiveDatabase().Returns(TransientDatabase.DatabaseB);
            });

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await azureDatabaseManager.Received().UpdateDatabaseSize(settings.DatabaseAName, AzureDatabaseSize.S0, Arg.Any<int?>());
        }

        [Test]
        public async Task RefreshData_WhenRefreshFails_DoesNotScaleActiveDatabaseToDormantSize()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DatabaseAName, "db-a")
                .With(s => s.DormantDatabaseSize, "S0")
                .Build();
            dataRefreshOrchestrator = BuildDataRefreshOrchestrator(settings);
            activeDatabaseProvider.GetActiveDatabase().Returns(TransientDatabase.DatabaseA);
            dataRefreshRunner.RefreshData(Arg.Any<int>(), Arg.Any<CancellationToken>()).Throws(new Exception());

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await azureDatabaseManager.DidNotReceive().UpdateDatabaseSize(settings.DatabaseAName, AzureDatabaseSize.S0, Arg.Any<int?>());
        }

        [Test]
        public async Task RefreshData_NotifiesOnSuccess()
        {
            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await dataRefreshCompletionNotifier.ReceivedWithAnyArgs().NotifyOfSuccess(default);
        }

        [Test]
        public async Task RefreshData_NotifiesOnFailure()
        {
            dataRefreshRunner.RefreshData(Arg.Any<int>(), Arg.Any<CancellationToken>()).Throws(new Exception());

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await dataRefreshCompletionNotifier.ReceivedWithAnyArgs().NotifyOfFailure(default);
        }

        #region Failures after the record is closed

        [Test]
        public async Task OrchestrateDataRefresh_WhenRefreshFailsAndCompletionMessageCannotBePublished_StillClosesRecordAsFailed()
        {
            GivenTheRefreshFails();
            GivenNoCompletionMessageCanBePublished();

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await dataRefreshHistoryRepository.Received()
                .UpdateExecutionDetails(DefaultRecordId, Arg.Any<string>(), Arg.Is<DateTime?>(finishTime => finishTime.HasValue));
            await dataRefreshHistoryRepository.Received().UpdateSuccessFlag(DefaultRecordId, false);
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenRefreshFailsAndCompletionMessageCannotBePublished_DoesNotThrow()
        {
            // A throw makes Service Bus deliver the request again. While the record stayed open, each delivery ran the failed
            // refresh again, until the request was dead-lettered.
            GivenTheRefreshFails();
            GivenNoCompletionMessageCanBePublished();

            var act = () => dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await act.Should().NotThrowAsync();
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenRefreshFailsAndCompletionMessageCannotBePublished_LogsThePublishFailure()
        {
            GivenTheRefreshFails();
            var publishFailure = GivenNoCompletionMessageCanBePublished();

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            logger.Received().SendTrace(Arg.Is<string>(message => message.Contains(publishFailure)), LogLevel.Error);
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenRefreshFails_ClosesRecordBeforeNotifyingOfFailure()
        {
            // The notifications must only go out for a closed record. If they went out first, a failure to send them could
            // stop the record from being closed.
            GivenTheRefreshFails();

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            Received.InOrder(() =>
            {
                dataRefreshHistoryRepository.UpdateSuccessFlag(DefaultRecordId, false);
                dataRefreshCompletionNotifier.NotifyOfFailure(DefaultRecordId);
            });
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenSuccessMessageCannotBePublished_DoesNotStoreRecordAsFailed()
        {
            // Only the success publish fails, as in a short Service Bus outage. Then a later failure publish can work, and the
            // record can be marked as failed after it was marked successful. That would send searches back to the old database.
            dataRefreshCompletionNotifier.NotifyOfSuccess(Arg.Any<int>()).ThrowsAsync(new Exception(fixture.Create<string>()));

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await dataRefreshHistoryRepository.Received().UpdateSuccessFlag(DefaultRecordId, true);
            await dataRefreshHistoryRepository.DidNotReceive().UpdateSuccessFlag(DefaultRecordId, false);
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenSuccessMessageCannotBePublished_DoesNotReportRefreshAsFailed()
        {
            GivenNoCompletionMessageCanBePublished();

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await dataRefreshCompletionNotifier.DidNotReceiveWithAnyArgs().NotifyOfFailure(default);
            logger.DidNotReceive().SendTrace(Arg.Any<string>(), LogLevel.Critical);
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenSuccessMessageCannotBePublished_LogsThePublishFailure()
        {
            var publishFailure = GivenNoCompletionMessageCanBePublished();

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            logger.Received().SendTrace(Arg.Is<string>(message => message.Contains(publishFailure)), LogLevel.Error);
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenSuccessMessageCannotBePublished_DoesNotThrow()
        {
            GivenNoCompletionMessageCanBePublished();

            var act = () => dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await act.Should().NotThrowAsync();
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenOldDatabaseCannotBeScaledDown_DoesNotStoreRecordAsFailed()
        {
            // The record is already closed as successful, and the refreshed database is already the active one. Marking the
            // record as failed would send searches back to the old database.
            GivenTheOldDatabaseCannotBeScaledDown();

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await dataRefreshHistoryRepository.Received().UpdateSuccessFlag(DefaultRecordId, true);
            await dataRefreshHistoryRepository.DidNotReceive().UpdateSuccessFlag(DefaultRecordId, false);
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenOldDatabaseCannotBeScaledDown_SendsTeardownFailureAlert()
        {
            GivenTheOldDatabaseCannotBeScaledDown();

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await dataRefreshSupportNotificationSender.Received().SendTeardownFailureAlert(DefaultRecordId);
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenOldDatabaseCannotBeScaledDown_StillNotifiesOfSuccess()
        {
            GivenTheOldDatabaseCannotBeScaledDown();

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await dataRefreshCompletionNotifier.Received().NotifyOfSuccess(DefaultRecordId);
            await dataRefreshCompletionNotifier.DidNotReceiveWithAnyArgs().NotifyOfFailure(default);
        }

        private void GivenTheRefreshFails()
        {
            dataRefreshRunner.RefreshData(Arg.Any<int>(), Arg.Any<CancellationToken>()).Throws(new Exception(fixture.Create<string>()));
        }

        /// <summary>
        /// Every publish to the completion topic fails, as it does when the topic does not exist, or when Service Bus is still
        /// unavailable after all send retries.
        /// </summary>
        /// <returns>The message of the exception that each publish throws.</returns>
        private string GivenNoCompletionMessageCanBePublished()
        {
            var publishFailure = fixture.Create<string>();
            dataRefreshCompletionNotifier.NotifyOfSuccess(Arg.Any<int>()).ThrowsAsync(new Exception(publishFailure));
            dataRefreshCompletionNotifier.NotifyOfFailure(Arg.Any<int>()).ThrowsAsync(new Exception(publishFailure));
            return publishFailure;
        }

        private void GivenTheOldDatabaseCannotBeScaledDown()
        {
            azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), Arg.Any<AzureDatabaseSize>(), Arg.Any<int?>())
                .ThrowsAsync(new Exception(fixture.Create<string>()));
        }

        #endregion

        #region The donor genotype precomputation run

        [Test]
        public async Task OrchestrateDataRefresh_WhenRefreshFails_CancelsThePrecomputationRunOfTheRecord()
        {
            GivenTheRefreshFails();

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await precomputationRunCanceller.Received(1).CancelRun(Arg.Is<DataRefreshRecord>(r => r.Id == DefaultRecordId));
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenRefreshFails_CancelsThePrecomputationRunAfterTheFailureIsReported()
        {
            // The removal of the staging data waits for the statements of the workers, so it must not delay the record or the
            // notifications.
            GivenTheRefreshFails();

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            Received.InOrder(() =>
            {
                dataRefreshHistoryRepository.UpdateSuccessFlag(DefaultRecordId, false);
                dataRefreshCompletionNotifier.NotifyOfFailure(DefaultRecordId);
                precomputationRunCanceller.CancelRun(Arg.Is<DataRefreshRecord>(r => r.Id == DefaultRecordId));
            });
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenJobSuccessful_DoesNotCancelThePrecomputationRun()
        {
            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await precomputationRunCanceller.DidNotReceiveWithAnyArgs().CancelRun(null);
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenLeaseIsLostMidRun_DoesNotCancelThePrecomputationRun()
        {
            // The invocation that took the lease over continues the stage, and with it the run.
            GivenTheLeaseIsLostDuringA(refresh: token => token.ThrowIfCancellationRequested());

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await precomputationRunCanceller.DidNotReceiveWithAnyArgs().CancelRun(null);
        }

        #endregion

        #region Run-level lease

        [Test]
        public async Task OrchestrateDataRefresh_WhenLeaseCannotBeClaimed_DoesNotRunAnyStage()
        {
            RefuseTheLeaseClaim();

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await dataRefreshRunner.DidNotReceiveWithAnyArgs().RefreshData(default, default);
            await dataRefreshHistoryRepository.DidNotReceiveWithAnyArgs().UpdateRunAttemptDetails(default);
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenLeaseCannotBeClaimed_DoesNotThrow()
        {
            // Throwing here would propagate out of the function and trigger the Service Bus redelivery that the lease
            // exists to make harmless.
            RefuseTheLeaseClaim();

            await dataRefreshOrchestrator.Invoking(o => o.OrchestrateDataRefresh(DefaultRecordId)).Should().NotThrowAsync();
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenLeaseCannotBeClaimed_DoesNotNotifySupportOfRunInProgress()
        {
            RefuseTheLeaseClaim();

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await dataRefreshSupportNotificationSender.DidNotReceiveWithAnyArgs().SendInProgressNotification(default, default);
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenLeaseCannotBeClaimed_DoesNotInspectIncompleteJobRecords()
        {
            // The claim must come first. A request naming an already-completed record would otherwise reach
            // FetchIncompleteJobRecord, which throws, dead-letters, and triggers a cleanup that scales down whichever
            // database is dormant - potentially in the middle of a live refresh.
            RefuseTheLeaseClaim();

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            dataRefreshHistoryRepository.DidNotReceive().GetIncompleteRefreshJobs();
        }

        [Test]
        public async Task OrchestrateDataRefresh_ClaimsLeaseAgainstTheRequestedRecord()
        {
            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await dataRefreshHistoryRepository.Received().TryClaimRefreshLease(
                DefaultRecordId, Arg.Any<Guid>(), Arg.Any<DateTime>(), TimeSpan.FromMinutes(30));
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenJobSuccessful_ReleasesLease()
        {
            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await dataRefreshHistoryRepository.Received().ReleaseRefreshLease(DefaultRecordId, Arg.Any<Guid>());
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenDataRefreshFails_ReleasesLease()
        {
            dataRefreshRunner.RefreshData(Arg.Any<int>(), Arg.Any<CancellationToken>()).Throws(new Exception());

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await dataRefreshHistoryRepository.Received().ReleaseRefreshLease(DefaultRecordId, Arg.Any<Guid>());
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenOrchestrationThrows_StillReleasesLease()
        {
            // A refresh that ends by throwing is retried by Service Bus. If the lease were not released, the retry would
            // arrive as a new invocation, be refused, and silently do nothing until the lease expired.
            dataRefreshHistoryRepository.GetIncompleteRefreshJobs().Returns(DataRefreshRecordBuilder.New.Build(2));

            await dataRefreshOrchestrator.Invoking(o => o.OrchestrateDataRefresh(DefaultRecordId)).Should().ThrowAsync<AtlasHttpException>();

            await dataRefreshHistoryRepository.Received().ReleaseRefreshLease(DefaultRecordId, Arg.Any<Guid>());
        }

        [Test]
        public async Task OrchestrateDataRefresh_ReleasesLeaseHeldByTheClaimingInvocation()
        {
            Guid? claimedBy = null;
            dataRefreshHistoryRepository
                .TryClaimRefreshLease(Arg.Any<int>(), Arg.Do<Guid>(owner => claimedBy = owner), Arg.Any<DateTime>(), Arg.Any<TimeSpan>())
                .Returns(true);

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            claimedBy.Should().NotBeNull();
            await dataRefreshHistoryRepository.Received().ReleaseRefreshLease(DefaultRecordId, claimedBy.Value);
        }

        [Test]
        public async Task OrchestrateDataRefresh_ClaimsLeaseUnderTheInvocationIdSuppliedByTheCaller()
        {
            // RunDataRefresh logs this id on arrival and passes it here. The two only corroborate each other if the same
            // value reaches the LeaseOwner column, which is what lets telemetry and the record be read together to
            // establish whether invocations overlapped.
            var invocationId = Guid.NewGuid();

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId, invocationId);

            await dataRefreshHistoryRepository.Received().TryClaimRefreshLease(
                DefaultRecordId, invocationId, Arg.Any<DateTime>(), Arg.Any<TimeSpan>());
            await dataRefreshHistoryRepository.Received().ReleaseRefreshLease(DefaultRecordId, invocationId);
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenLeaseIsLostMidRun_CancelsTheRefresh()
        {
            var wasCancelled = false;
            GivenTheLeaseIsLostDuringA(refresh: token =>
            {
                wasCancelled = true;
                token.ThrowIfCancellationRequested();
            });

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            wasCancelled.Should().BeTrue();
        }

        [Test]
        public async Task OrchestrateDataRefresh_WhenLeaseIsLostMidRun_DoesNotCloseOutTheRecord()
        {
            // The record now belongs to whichever invocation took the lease over. Marking it complete, or reporting a
            // failure against it, would sabotage that invocation's run.
            GivenTheLeaseIsLostDuringA(refresh: token => token.ThrowIfCancellationRequested());

            await dataRefreshOrchestrator.OrchestrateDataRefresh(DefaultRecordId);

            await dataRefreshHistoryRepository.DidNotReceiveWithAnyArgs().UpdateSuccessFlag(default, default);
            await dataRefreshCompletionNotifier.DidNotReceiveWithAnyArgs().NotifyOfFailure(default);
            await dataRefreshCompletionNotifier.DidNotReceiveWithAnyArgs().NotifyOfSuccess(default);
        }

        [TestCase(0, 60, TestName = "Zero lease duration")]
        [TestCase(30, 0, TestName = "Zero renewal interval")]
        [TestCase(1, 60, TestName = "Renewal interval equal to lease duration")]
        [TestCase(1, 45, TestName = "Fewer than two renewal attempts per lease")]
        public async Task OrchestrateDataRefresh_WithInvalidLeaseTimings_ThrowsWithoutRunningAnyStage(
            int leaseDurationMinutes,
            int renewalIntervalSeconds)
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.LeaseDurationMinutes, leaseDurationMinutes)
                .With(s => s.LeaseRenewalIntervalSeconds, renewalIntervalSeconds)
                .Build();
            dataRefreshOrchestrator = BuildDataRefreshOrchestrator(settings);

            await dataRefreshOrchestrator.Invoking(o => o.OrchestrateDataRefresh(DefaultRecordId)).Should().ThrowAsync<Exception>();

            await dataRefreshRunner.DidNotReceiveWithAnyArgs().RefreshData(default, default);
            await dataRefreshHistoryRepository.DidNotReceiveWithAnyArgs().TryClaimRefreshLease(default, default, default, default);
        }

        private void RefuseTheLeaseClaim()
        {
            dataRefreshHistoryRepository.TryClaimRefreshLease(default, default, default, default).ReturnsForAnyArgs(false);
        }

        /// <summary>
        /// Sets up an invocation whose renewals are all refused - i.e. it has been fenced by another invocation - and
        /// whose refresh runs until the resulting cancellation reaches it.
        /// </summary>
        /// <remarks>
        /// Uses the shortest timings the orchestrator will accept, so that the heartbeat ticks within the lifetime of a
        /// unit test rather than after the production default of a minute.
        /// </remarks>
        private void GivenTheLeaseIsLostDuringA(Action<CancellationToken> refresh)
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.LeaseDurationMinutes, 1)
                .With(s => s.LeaseRenewalIntervalSeconds, 1)
                .Build();
            dataRefreshOrchestrator = BuildDataRefreshOrchestrator(settings);

            dataRefreshHistoryRepository.TryRenewRefreshLease(default, default, default).ReturnsForAnyArgs(false);

            dataRefreshRunner.RefreshData(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(call =>
            {
                var cancellationToken = call.Arg<CancellationToken>();
                cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(30));
                refresh(cancellationToken);
                return NewHlaVersion;
            });
        }

        #endregion
    }
}