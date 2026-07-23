using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Client.Models.SupportMessages;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.Notifications;
using Atlas.Functions.Models;
using Atlas.Functions.Services;
using Atlas.Functions.Services.BlobStorageClients;
using Atlas.Functions.Settings;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.SearchTracking.Common.Dispatchers;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;
using AutoFixture;
using NSubstitute;
using NUnit.Framework;
using Microsoft.Extensions.Options;

namespace Atlas.Functions.Test.Services
{
    [TestFixture]
    internal class ParallelMatchPredictionCompletionServiceTests
    {
        // The orchestration stage that the parallel match-prediction path reports on failure. Asserted as a literal
        // because it is a downstream contract value, not incidental test data — so it is deliberately not randomised.
        private const string MatchPredictionBatchProcessingStage = "MatchPredictionBatchProcessing";

        // The notification source the parallel match-prediction path stamps on alerts/notifications. Mirrors the private
        // const in ParallelMatchPredictionCompletionService; asserted as a literal for the same contract reason.
        private const string ParallelMatchPredictionNotificationSource = "Atlas.Functions.ParallelMatchPrediction";

        private IParallelMatchPredictionRepository repository;
        private ISearchCompletionMessageSender searchCompletionMessageSender;
        private IMatchPredictionSearchTrackingDispatcher trackingDispatcher;
        private INotificationSender notificationSender;
        private IMatchingResultsDownloader matchingResultsDownloader;
        private IResultsCombiner resultsCombiner;
        private ParallelMatchPredictionCompletionService completionService;

        private Fixture fixture;

        [SetUp]
        public void SetUp()
        {
            fixture = new Fixture();
            repository = Substitute.For<IParallelMatchPredictionRepository>();
            searchCompletionMessageSender = Substitute.For<ISearchCompletionMessageSender>();
            trackingDispatcher = Substitute.For<IMatchPredictionSearchTrackingDispatcher>();
            notificationSender = Substitute.For<INotificationSender>();
            matchingResultsDownloader = Substitute.For<IMatchingResultsDownloader>();
            resultsCombiner = Substitute.For<IResultsCombiner>();

            completionService = new ParallelMatchPredictionCompletionService(
                repository,
                matchingResultsDownloader,
                resultsCombiner,
                Substitute.For<ISearchResultsBlobStorageClient>(),
                searchCompletionMessageSender,
                trackingDispatcher,
                notificationSender,
                Substitute.For<ISearchLogger<SearchLoggingContext>>(),
                Options.Create(new AzureStorageSettings()),
                Options.Create(new OrchestrationSettings())
            );
        }

        /// <summary>
        /// Stubs the collaborators the success path dereferences so <see cref="ParallelMatchPredictionCompletionService.FinaliseRun"/>
        /// can run end-to-end for a run with no failed batches (the downloaders/combiner are otherwise substitutes
        /// returning null, which would NRE inside the pipeline).
        /// </summary>
        private void StubSuccessfulCompletionPipeline()
        {
            matchingResultsDownloader.DownloadSummary(Arg.Any<string>(), Arg.Any<bool>())
                .Returns(new OriginalMatchingAlgorithmResultSet());
            resultsCombiner.BuildResultsSummary(
                    Arg.Any<ResultSet<MatchingAlgorithmResult>>(),
                    Arg.Any<TimeSpan>(),
                    Arg.Any<TimeSpan>())
                .Returns(new OriginalSearchResultSet { SearchRequestId = fixture.Create<Guid>().ToString() });
        }

        [Test]
        public async Task AbandonRun_WhenRunTransitionedToAbandoned_PublishesSingleFailureNotificationWithDetail()
        {
            var searchId = fixture.Create<Guid>();
            repository.TryMarkRunAsAbandoned(Arg.Any<int>(), Arg.Any<DateTime>())
                .Returns(new AbandonedRunHeader(searchId, null, false, fixture.Create<int>()));

            await completionService.AbandonRun(fixture.Create<int>());

            await searchCompletionMessageSender.Received(1).PublishFailureMessage(
                Arg.Is<SendFailureNotificationParameters>(p =>
                    p.SearchRequestId == searchId.ToString()
                    && p.RepeatSearchRequestId == null
                    && p.StageReached == MatchPredictionBatchProcessingStage
                    && !string.IsNullOrEmpty(p.FailureDetail))
            );
        }

        [Test]
        public async Task AbandonRun_RepeatSearch_PublishesNotificationWithRepeatSearchId()
        {
            var searchId = fixture.Create<Guid>();
            var repeatId = fixture.Create<Guid>();
            repository.TryMarkRunAsAbandoned(Arg.Any<int>(), Arg.Any<DateTime>())
                .Returns(new AbandonedRunHeader(searchId, repeatId, true, fixture.Create<int>()));

            await completionService.AbandonRun(fixture.Create<int>());

            await searchCompletionMessageSender.Received(1).PublishFailureMessage(
                Arg.Is<SendFailureNotificationParameters>(p =>
                    p.SearchRequestId == searchId.ToString()
                    && p.RepeatSearchRequestId == repeatId.ToString())
            );
        }

        [Test]
        public async Task AbandonRun_WhenRunTransitionedToAbandoned_EmitsAbandonedProcessCompletedTrackingEvent()
        {
            var searchId = fixture.Create<Guid>();
            var totalBatchCount = fixture.Create<int>();
            repository.TryMarkRunAsAbandoned(Arg.Any<int>(), Arg.Any<DateTime>())
                .Returns(new AbandonedRunHeader(searchId, null, false, totalBatchCount));

            await completionService.AbandonRun(fixture.Create<int>());

            await trackingDispatcher.Received(1).ProcessCompleted(
                Arg.Is<(Guid SearchIdentifier, Guid? OriginalSearchIdentifier, bool IsSuccessful, MatchPredictionFailureInfo FailureInfo, int? DonorsPerBatch, int? TotalNumberOfBatches)>(
                    e => e.SearchIdentifier == searchId
                      && e.OriginalSearchIdentifier == null
                      && e.IsSuccessful == false
                      && e.FailureInfo.Type == MatchPredictionFailureType.Abandoned
                      && e.TotalNumberOfBatches == totalBatchCount)
            );
        }

        [Test]
        public async Task AbandonRun_RepeatSearch_EmitsTrackingEventWithRepeatAsPrimaryAndOriginalAsSecondary()
        {
            var searchId = fixture.Create<Guid>();
            var repeatId = fixture.Create<Guid>();
            repository.TryMarkRunAsAbandoned(Arg.Any<int>(), Arg.Any<DateTime>())
                .Returns(new AbandonedRunHeader(searchId, repeatId, true, fixture.Create<int>()));

            await completionService.AbandonRun(fixture.Create<int>());

            await trackingDispatcher.Received(1).ProcessCompleted(
                Arg.Is<(Guid SearchIdentifier, Guid? OriginalSearchIdentifier, bool IsSuccessful, MatchPredictionFailureInfo FailureInfo, int? DonorsPerBatch, int? TotalNumberOfBatches)>(
                    e => e.SearchIdentifier == repeatId
                      && e.OriginalSearchIdentifier == searchId
                      && e.IsSuccessful == false)
            );
        }

        [Test]
        public async Task AbandonRun_WhenRunTransitionedToAbandoned_RaisesAlert()
        {
            var searchId = fixture.Create<Guid>();
            repository.TryMarkRunAsAbandoned(Arg.Any<int>(), Arg.Any<DateTime>())
                .Returns(new AbandonedRunHeader(searchId, null, false, fixture.Create<int>()));

            await completionService.AbandonRun(fixture.Create<int>());

            await notificationSender.Received(1).SendAlert(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Priority>(),
                Arg.Any<string>()
            );
        }

        [Test]
        public async Task AbandonRun_WhenRunNoLongerRunning_DoesNotNotifyTrackOrAlert()
        {
            repository.TryMarkRunAsAbandoned(Arg.Any<int>(), Arg.Any<DateTime>())
                .Returns((AbandonedRunHeader)null);

            await completionService.AbandonRun(fixture.Create<int>());

            await searchCompletionMessageSender.DidNotReceiveWithAnyArgs()
                .PublishFailureMessage(Arg.Any<SendFailureNotificationParameters>());
            await trackingDispatcher.DidNotReceiveWithAnyArgs().ProcessCompleted(default);
            await notificationSender.DidNotReceiveWithAnyArgs()
                .SendAlert(default, default, default, default);
        }

        [Test]
        public async Task FinaliseRun_LateFailureReplayForAbandonedRun_RepublishesFailureWithRealCauseAndMarksFailed()
        {
            var runId = fixture.Create<int>();
            repository.GetRunWithResults(runId).Returns(RunResults(
                status: ParallelMatchPredictionRunStatus.Abandoned,
                failedBatch: true
            ));

            await completionService.FinaliseRun(runId);

            // The provisional abandonment failure is superseded once every batch has reported: the definitive
            // failure is re-published, now carrying the real batch-failure cause rather than the generic timeout.
            await searchCompletionMessageSender.Received(1).PublishFailureMessage(
                Arg.Is<SendFailureNotificationParameters>(p => !string.IsNullOrEmpty(p.FailureDetail))
            );
            await trackingDispatcher.Received(1).ProcessCompleted(
                Arg.Is<(Guid SearchIdentifier, Guid? OriginalSearchIdentifier, bool IsSuccessful, MatchPredictionFailureInfo FailureInfo, int? DonorsPerBatch, int? TotalNumberOfBatches)>(
                    e => e.IsSuccessful == false
                      && e.FailureInfo.Type == MatchPredictionFailureType.BatchWorkerFailure)
            );
            // The run lands in the accurate FailedDuringBatchProcessing state (MarkRunFailed also accepts Abandoned).
            await repository.Received(1).MarkRunFailed(runId, Arg.Any<DateTime>());
        }

        [Test]
        public async Task FinaliseRun_FailedRunThatWasNotAbandoned_PublishesFailureNotificationAndMarksFailed()
        {
            var runId = fixture.Create<int>();
            repository.GetRunWithResults(runId).Returns(RunResults(
                status: ParallelMatchPredictionRunStatus.Running,
                failedBatch: true
            ));

            await completionService.FinaliseRun(runId);

            await searchCompletionMessageSender.Received(1).PublishFailureMessage(
                Arg.Is<SendFailureNotificationParameters>(p => !string.IsNullOrEmpty(p.FailureDetail))
            );
            await trackingDispatcher.Received(1).ProcessCompleted(
                Arg.Is<(Guid SearchIdentifier, Guid? OriginalSearchIdentifier, bool IsSuccessful, MatchPredictionFailureInfo FailureInfo, int? DonorsPerBatch, int? TotalNumberOfBatches)>(
                    e => e.IsSuccessful == false
                      && e.FailureInfo.Type == MatchPredictionFailureType.BatchWorkerFailure)
            );
            await repository.Received(1).MarkRunFailed(runId, Arg.Any<DateTime>());
        }

        [Test]
        public async Task FinaliseRun_FailedRunThatWasNotAbandoned_RaisesHighPriorityAlert()
        {
            var runId = fixture.Create<int>();
            repository.GetRunWithResults(runId).Returns(RunResults(
                status: ParallelMatchPredictionRunStatus.Running,
                failedBatch: true
            ));

            await completionService.FinaliseRun(runId);

            await notificationSender.Received(1).SendAlert(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Priority.High,
                Arg.Any<string>()
            );
        }

        [Test]
        public async Task FinaliseRun_FailedRun_PublishesFailureDetailCarryingBatchReason()
        {
            var runId = fixture.Create<int>();
            var batchFailureMessage = fixture.Create<string>();
            repository.GetRunWithResults(runId).Returns(RunResults(
                status: ParallelMatchPredictionRunStatus.Running,
                failedBatch: true,
                batchFailureMessage: batchFailureMessage
            ));

            await completionService.FinaliseRun(runId);

            await searchCompletionMessageSender.Received(1).PublishFailureMessage(
                Arg.Is<SendFailureNotificationParameters>(p => p.FailureDetail.Contains(batchFailureMessage))
            );
        }

        [Test]
        public async Task FinaliseRun_SuccessfulRun_SendsSuccessNotificationToNotificationsTopic()
        {
            var runId = fixture.Create<int>();
            repository.GetRunWithResults(runId).Returns(RunResults(
                status: ParallelMatchPredictionRunStatus.Running,
                failedBatch: false
            ));
            StubSuccessfulCompletionPipeline();

            await completionService.FinaliseRun(runId);

            // The success path must emit a notification (the positive counterpart to the failure alert) and finalise.
            await notificationSender.Received(1).SendNotification(
                Arg.Any<string>(),
                Arg.Any<string>(),
                ParallelMatchPredictionNotificationSource
            );
            await repository.Received(1).MarkRunFinalised(runId, Arg.Any<DateTime>());
        }

        [Test]
        public async Task FinaliseRun_RecoveredRunFullyReplayedToSuccess_SendsSuccessNotification()
        {
            var runId = fixture.Create<int>();
            // A run that had previously failed during batch processing, whose every failed batch has since been replayed
            // to success (no failed batches remain), is driven to success and must notify like any other success.
            repository.GetRunWithResults(runId).Returns(RunResults(
                status: ParallelMatchPredictionRunStatus.FailedDuringBatchProcessing,
                failedBatch: false
            ));
            StubSuccessfulCompletionPipeline();

            await completionService.FinaliseRun(runId);

            await notificationSender.Received(1).SendNotification(
                Arg.Any<string>(),
                Arg.Any<string>(),
                ParallelMatchPredictionNotificationSource
            );
            await repository.Received(1).MarkRunFinalised(runId, Arg.Any<DateTime>());
        }

        [Test]
        public async Task FinaliseRun_FailedRun_DoesNotSendSuccessNotification()
        {
            var runId = fixture.Create<int>();
            repository.GetRunWithResults(runId).Returns(RunResults(
                status: ParallelMatchPredictionRunStatus.Running,
                failedBatch: true
            ));

            await completionService.FinaliseRun(runId);

            // A failed run raises a High-priority alert, never a success notification.
            await notificationSender.DidNotReceiveWithAnyArgs().SendNotification(default, default, default);
        }

        private ParallelMatchPredictionRunResults RunResults(
            ParallelMatchPredictionRunStatus status, bool failedBatch, string batchFailureMessage = null)
        {
            return new ParallelMatchPredictionRunResults
            {
                Run = new ParallelMatchPredictionRun
                {
                    SearchIdentifier = fixture.Create<Guid>(),
                    IsRepeatSearch = false,
                    RepeatSearchIdentifier = null,
                    TotalBatchCount = fixture.Create<int>(),
                    Status = status,
                    SearchInitiatedTimeUtc = fixture.Create<DateTime>(),
                },
                BatchResultLocations = new List<string>(),
                FailedBatches = failedBatch
                    ? new List<ParallelMatchPredictionBatch>
                    {
                        new()
                        {
                            BatchSequenceNumber = 0,
                            BatchStatus = ParallelMatchPredictionBatchStatus.Failed,
                            FailureMessage = batchFailureMessage ?? fixture.Create<string>(),
                            FailureException = fixture.Create<string>(),
                        }
                    }
                    : new List<ParallelMatchPredictionBatch>(),
            };
        }
    }
}
