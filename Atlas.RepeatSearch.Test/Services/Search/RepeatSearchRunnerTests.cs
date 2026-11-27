using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.DonorImport.ExternalInterface;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchRequests;
using Atlas.RepeatSearch.Clients;
using Atlas.RepeatSearch.Data.Models;
using Atlas.RepeatSearch.Data.Repositories;
using Atlas.RepeatSearch.Data.Settings;
using Atlas.RepeatSearch.Models;
using Atlas.RepeatSearch.Services.ResultSetTracking;
using Atlas.RepeatSearch.Services.Search;
using Atlas.RepeatSearch.Settings;
using Atlas.RepeatSearch.Settings.Azure;
using Atlas.RepeatSearch.Settings.ServiceBus;
using Atlas.RepeatSearch.Test.TestHelpers.Builders;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;
using AutoFixture;
using AwesomeAssertions;
using FluentValidation;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using MatchingAlgorithmFailureInfo = Atlas.SearchTracking.Common.Models.MatchingAlgorithmFailureInfo;

namespace Atlas.RepeatSearch.Test.Services.Search
{
    [TestFixture]
    public class RepeatSearchRunnerTests
    {
        private const int WaitIntervalSeconds = 300;
        private const int MaxWaitMinutes = 1440;

        private Fixture fixture;
        private string originalSearchId;
        private string repeatSearchId;

        private ISearchService searchService;
        private IRepeatSearchValidator repeatSearchValidator;
        private ISearchTrackingEventPublisher searchTrackingEventPublisher;
        private IRepeatSearchMatchingFailureNotificationSender failureNotificationSender;
        private IRepeatSearchServiceBusClient serviceBusClient;
        private ISearchResultsBlobStorageClient resultsBlobStorageClient;
        private IRepeatSearchHistoryRepository historyRepository;
        private IRepeatSearchDifferentialCalculator differentialCalculator;
        private IOriginalSearchResultSetTracker resultSetTracker;

        private IRepeatSearchRunner repeatSearchRunner;

        [SetUp]
        public void SetUp()
        {
            fixture = new Fixture();
            // The runner reads both ids as GUIDs.
            originalSearchId = fixture.Create<Guid>().ToString();
            repeatSearchId = fixture.Create<Guid>().ToString();

            searchService = Substitute.For<ISearchService>();
            repeatSearchValidator = Substitute.For<IRepeatSearchValidator>();
            searchTrackingEventPublisher = Substitute.For<ISearchTrackingEventPublisher>();
            failureNotificationSender = Substitute.For<IRepeatSearchMatchingFailureNotificationSender>();
            serviceBusClient = Substitute.For<IRepeatSearchServiceBusClient>();
            resultsBlobStorageClient = Substitute.For<ISearchResultsBlobStorageClient>();
            historyRepository = Substitute.For<IRepeatSearchHistoryRepository>();
            differentialCalculator = Substitute.For<IRepeatSearchDifferentialCalculator>();
            resultSetTracker = Substitute.For<IOriginalSearchResultSetTracker>();

            repeatSearchValidator.IsOriginalSearchResultSetStored(default).ReturnsForAnyArgs(true);
            searchService.Search(default, default)
                .ReturnsForAnyArgs(Task.FromResult<IEnumerable<MatchingAlgorithmResult>>(new List<MatchingAlgorithmResult>()));
            differentialCalculator.CalculateDifferential(default, default, default).ReturnsForAnyArgs(SearchResultDifferentialBuilder.New.Build());
            resultSetTracker.ApplySearchResultDiff(default, default).ReturnsForAnyArgs(true);

            repeatSearchRunner = BuildRunner(DefaultSettings());
        }

        private static RepeatSearchSettings DefaultSettings() => new()
        {
            OriginalResultsWaitIntervalSeconds = WaitIntervalSeconds,
            MaxWaitForOriginalResultsMinutes = MaxWaitMinutes
        };

        private IRepeatSearchRunner BuildRunner(RepeatSearchSettings repeatSearchSettings)
        {
            return new RepeatSearchRunner(
                serviceBusClient,
                searchService,
                resultsBlobStorageClient,
                Substitute.For<IMatchingAlgorithmSearchLogger>(),
                new MatchingAlgorithmSearchLoggingContext(),
                Substitute.For<IActiveHlaNomenclatureVersionAccessor>(),
                Substitute.For<IActiveDataRefreshRecordAccessor>(),
                historyRepository,
                repeatSearchValidator,
                differentialCalculator,
                resultSetTracker,
                new AzureStorageSettings(),
                new MessagingServiceBusSettings { RepeatSearchRequestsMaxDeliveryCount = 10 },
                failureNotificationSender,
                new MatchingAlgorithmSearchTrackingContext(),
                searchTrackingEventPublisher,
                repeatSearchSettings);
        }

        [Test]
        public async Task RunSearch_WhenRequestFailsValidation_PublishesInitiationBeforeCompletedWithValidationError()
        {
            repeatSearchValidator.ValidateRepeatSearchAndThrow(default)
                .ThrowsAsyncForAnyArgs(new ValidationException("no record of the original search"));

            await repeatSearchRunner.RunSearch(BuildRequest(), 1, default);

            AssertInitiationPublishedBeforeCompletedWithFailure(MatchingAlgorithmFailureType.ValidationError);
            await searchService.DidNotReceiveWithAnyArgs().Search(default, default);
        }

        [Test]
        public async Task RunSearch_WhenValidationThrowsUnexpectedError_PublishesInitiationBeforeCompletedWithUnexpectedError()
        {
            repeatSearchValidator.ValidateRepeatSearchAndThrow(default)
                .ThrowsAsyncForAnyArgs(new Exception("transient SQL error"));

            await repeatSearchRunner.Invoking(r => r.RunSearch(BuildRequest(), 1, default))
                .Should().ThrowAsync<Exception>();

            AssertInitiationPublishedBeforeCompletedWithFailure(MatchingAlgorithmFailureType.UnexpectedError);
            await searchService.DidNotReceiveWithAnyArgs().Search(default, default);
        }

        [Test]
        public async Task RunSearch_WhenFailureNotificationIsNotPublished_PublishesCompletedWithoutResultsSentTime()
        {
            repeatSearchValidator.ValidateRepeatSearchAndThrow(default)
                .ThrowsAsyncForAnyArgs(new Exception("transient SQL error"));
            failureNotificationSender.SendFailureNotification(default, default, default)
                .ThrowsAsyncForAnyArgs(new Exception("Service Bus error"));

            await repeatSearchRunner.Invoking(r => r.RunSearch(BuildRequest(), 1, default))
                .Should().ThrowAsync<Exception>();

            await searchTrackingEventPublisher.Received(1).ProcessCompleted(
                Arg.Is<(string, DateTime?, int?, MatchingAlgorithmFailureInfo, MatchingAlgorithmRepeatSearchResultsDetails, int?)>(e =>
                    e.Item2 == null));
        }

        [Test]
        public async Task RunSearch_WhenOriginalResultSetIsStored_ContinuesWithoutWaiting()
        {
            // Stops the run at validation, the step after the wait check: the steps after it are not needed here.
            repeatSearchValidator.ValidateRepeatSearchAndThrow(default)
                .ThrowsAsyncForAnyArgs(new ValidationException("stop after the wait check"));

            await repeatSearchRunner.RunSearch(BuildRequest(), 1, DateTimeOffset.UtcNow);

            await serviceBusClient.DidNotReceiveWithAnyArgs().PublishToRepeatSearchRequestsTopic(default, default);
            await searchTrackingEventPublisher.ReceivedWithAnyArgs(1).ProcessInitiation(default, default);
            await repeatSearchValidator.ReceivedWithAnyArgs(1).ValidateRepeatSearchAndThrow(default);
        }

        [Test]
        public async Task RunSearch_WhenOriginalResultSetIsNotStoredBeforeMaxWait_PublishesRequestAgainWithDelay()
        {
            repeatSearchValidator.IsOriginalSearchResultSetStored(default).ReturnsForAnyArgs(false);
            var enqueuedTime = DateTimeOffset.UtcNow.AddMinutes(-10);

            await repeatSearchRunner.RunSearch(BuildRequest(), 1, enqueuedTime);

            await serviceBusClient.Received(1).PublishToRepeatSearchRequestsTopic(
                Arg.Is<IdentifiedRepeatSearchRequest>(r => r.RepeatSearchId == repeatSearchId && r.FirstRequestTimeUtc == enqueuedTime),
                Arg.Is<DateTimeOffset?>(t => t.HasValue &&
                                             t.Value > DateTimeOffset.UtcNow.AddSeconds(WaitIntervalSeconds - 60) &&
                                             t.Value <= DateTimeOffset.UtcNow.AddSeconds(WaitIntervalSeconds)));
        }

        [Test]
        public async Task RunSearch_WhenOriginalResultSetIsNotStoredBeforeMaxWait_DoesNotRunOrTrackOrNotify()
        {
            repeatSearchValidator.IsOriginalSearchResultSetStored(default).ReturnsForAnyArgs(false);

            await repeatSearchRunner.RunSearch(BuildRequest(), 1, DateTimeOffset.UtcNow);

            await searchTrackingEventPublisher.DidNotReceiveWithAnyArgs().ProcessInitiation(default, default);
            await searchTrackingEventPublisher.DidNotReceiveWithAnyArgs().ProcessCompleted(default);
            await failureNotificationSender.DidNotReceiveWithAnyArgs().SendFailureNotification(default, default, default);
            await repeatSearchValidator.DidNotReceiveWithAnyArgs().ValidateRepeatSearchAndThrow(default);
            await searchService.DidNotReceiveWithAnyArgs().Search(default, default);
        }

        [Test]
        public async Task RunSearch_WhenRequestHasWaitedBefore_KeepsFirstRequestTime()
        {
            repeatSearchValidator.IsOriginalSearchResultSetStored(default).ReturnsForAnyArgs(false);
            var firstRequestTime = DateTimeOffset.UtcNow.AddHours(-2);
            var request = BuildRequest();
            request.FirstRequestTimeUtc = firstRequestTime;

            await repeatSearchRunner.RunSearch(request, 1, DateTimeOffset.UtcNow);

            await serviceBusClient.Received(1).PublishToRepeatSearchRequestsTopic(
                Arg.Is<IdentifiedRepeatSearchRequest>(r => r.FirstRequestTimeUtc == firstRequestTime),
                Arg.Any<DateTimeOffset?>());
        }

        [Test]
        public async Task RunSearch_AfterWait_PublishesInitiationWithFirstRequestTime()
        {
            repeatSearchValidator.ValidateRepeatSearchAndThrow(default)
                .ThrowsAsyncForAnyArgs(new ValidationException("stop after initiation"));
            var firstRequestTime = DateTimeOffset.UtcNow.AddHours(-2);
            var request = BuildRequest();
            request.FirstRequestTimeUtc = firstRequestTime;

            await repeatSearchRunner.RunSearch(request, 1, DateTimeOffset.UtcNow);

            await searchTrackingEventPublisher.Received(1).ProcessInitiation(firstRequestTime.UtcDateTime, Arg.Any<DateTime>());
        }

        [Test]
        public async Task RunSearch_WhenOriginalResultSetIsNotStoredAfterMaxWait_FailsWithValidationError()
        {
            repeatSearchValidator.IsOriginalSearchResultSetStored(default).ReturnsForAnyArgs(false);
            repeatSearchValidator.ValidateRepeatSearchAndThrow(default)
                .ThrowsAsyncForAnyArgs(new ValidationException("no record of the original search"));
            var request = BuildRequest();
            request.FirstRequestTimeUtc = DateTimeOffset.UtcNow.AddMinutes(-MaxWaitMinutes);

            await repeatSearchRunner.RunSearch(request, 1, DateTimeOffset.UtcNow);

            await serviceBusClient.DidNotReceiveWithAnyArgs().PublishToRepeatSearchRequestsTopic(default, default);
            await failureNotificationSender.Received(1).SendFailureNotification(
                Arg.Any<IdentifiedRepeatSearchRequest>(), 1, 0, "no record of the original search");
            AssertInitiationPublishedBeforeCompletedWithFailure(MatchingAlgorithmFailureType.ValidationError);
        }

        [Test]
        public async Task RunSearch_WhenWaitSettingsAreNotSet_DoesNotWaitAndRunsValidation()
        {
            // Both values 0, as in an app without the settings: the wait is off, and the repeat search works as before.
            var runnerWithoutWait = BuildRunner(new RepeatSearchSettings());
            repeatSearchValidator.IsOriginalSearchResultSetStored(default).ReturnsForAnyArgs(false);
            repeatSearchValidator.ValidateRepeatSearchAndThrow(default)
                .ThrowsAsyncForAnyArgs(new ValidationException("no record of the original search"));

            await runnerWithoutWait.RunSearch(BuildRequest(), 1, DateTimeOffset.UtcNow);

            await repeatSearchValidator.DidNotReceiveWithAnyArgs().IsOriginalSearchResultSetStored(default);
            await serviceBusClient.DidNotReceiveWithAnyArgs().PublishToRepeatSearchRequestsTopic(default, default);
            await repeatSearchValidator.ReceivedWithAnyArgs(1).ValidateRepeatSearchAndThrow(default);
            AssertInitiationPublishedBeforeCompletedWithFailure(MatchingAlgorithmFailureType.ValidationError);
        }

        [Test]
        public async Task RunSearch_WhenOriginalSearchIdIsMissing_FailsWithValidationErrorWithoutWaiting()
        {
            repeatSearchValidator.IsOriginalSearchResultSetStored(default).ReturnsForAnyArgs(false);
            repeatSearchValidator.ValidateRepeatSearchAndThrow(default)
                .ThrowsAsyncForAnyArgs(new ValidationException("'Original Search Id' must not be empty."));
            var request = BuildRequest();
            request.RepeatSearchRequest.OriginalSearchId = null;

            await repeatSearchRunner.RunSearch(request, 1, DateTimeOffset.UtcNow);

            await serviceBusClient.DidNotReceiveWithAnyArgs().PublishToRepeatSearchRequestsTopic(default, default);
            AssertInitiationPublishedBeforeCompletedWithFailure(MatchingAlgorithmFailureType.ValidationError);
        }

        [Test]
        public async Task RunSearch_ChangesSetAfterResultsNotificationIsPublished()
        {
            await repeatSearchRunner.RunSearch(BuildRequest(), 1, DateTimeOffset.UtcNow);

            Received.InOrder(() =>
            {
                differentialCalculator.CalculateDifferential(
                    Arg.Any<string>(), Arg.Any<List<MatchingAlgorithmResult>>(), Arg.Any<DateTimeOffset>());
                resultsBlobStorageClient.UploadResults(Arg.Any<ResultSet<MatchingAlgorithmResult>>(), Arg.Any<int>(), Arg.Any<string>());
                serviceBusClient.PublishToResultsNotificationTopic(Arg.Is<MatchingResultsNotification>(n => n.WasSuccessful));
                resultSetTracker.ApplySearchResultDiff(Arg.Any<RepeatSearchHistoryRecord>(), Arg.Any<SearchResultDifferential>());
            });
        }

        [Test]
        public async Task RunSearch_RecordsRepeatSearchWithIdsCutoffAndDiffCounts()
        {
            var request = BuildRequest();
            var searchCutoffDate = request.RepeatSearchRequest.SearchCutoffDate.Value;
            var diff = SearchResultDifferentialBuilder.New
                .WithNewResults(fixture.CreateMany<string>(1).ToArray())
                .WithUpdatedResults(fixture.CreateMany<string>(2).ToArray())
                .WithRemovedResults(fixture.CreateMany<string>(3).ToArray())
                .Build();
            differentialCalculator.CalculateDifferential(default, default, default).ReturnsForAnyArgs(diff);

            await repeatSearchRunner.RunSearch(request, 1, DateTimeOffset.UtcNow);

            await resultSetTracker.Received(1).ApplySearchResultDiff(
                Arg.Is<RepeatSearchHistoryRecord>(r =>
                    r.OriginalSearchRequestId == originalSearchId &&
                    r.RepeatSearchRequestId == repeatSearchId &&
                    r.SearchCutoffDate == searchCutoffDate &&
                    r.AddedResultCount == 1 &&
                    r.UpdatedResultCount == 2 &&
                    r.RemovedResultCount == 3),
                diff);
        }

        [Test]
        public async Task RunSearch_WhenUploadFails_DoesNotChangeSetAndSendsFailureNotification()
        {
            resultsBlobStorageClient.UploadResults(Arg.Any<ResultSet<MatchingAlgorithmResult>>(), Arg.Any<int>(), Arg.Any<string>())
                .ThrowsAsync(new Exception(fixture.Create<string>()));

            var act = () => repeatSearchRunner.RunSearch(BuildRequest(), 1, DateTimeOffset.UtcNow);

            await act.Should().ThrowAsync<Exception>();
            await serviceBusClient.DidNotReceiveWithAnyArgs().PublishToResultsNotificationTopic(default);
            await resultSetTracker.DidNotReceiveWithAnyArgs().ApplySearchResultDiff(default, default);
            await failureNotificationSender.ReceivedWithAnyArgs(1).SendFailureNotification(default, default, default);
        }

        [Test]
        public async Task RunSearch_WhenPublishFails_DoesNotChangeSetAndSendsFailureNotification()
        {
            serviceBusClient.PublishToResultsNotificationTopic(default).ThrowsAsyncForAnyArgs(new Exception(fixture.Create<string>()));

            var act = () => repeatSearchRunner.RunSearch(BuildRequest(), 1, DateTimeOffset.UtcNow);

            await act.Should().ThrowAsync<Exception>();
            await resultSetTracker.DidNotReceiveWithAnyArgs().ApplySearchResultDiff(default, default);
            await failureNotificationSender.ReceivedWithAnyArgs(1).SendFailureNotification(default, default, default);
        }

        [Test]
        public async Task RunSearch_WhenSetChangeAndHistoryStepFails_ThrowsWithoutFailureNotification()
        {
            resultSetTracker.ApplySearchResultDiff(default, default).ThrowsAsyncForAnyArgs(new Exception(fixture.Create<string>()));

            var act = () => repeatSearchRunner.RunSearch(BuildRequest(), 1, DateTimeOffset.UtcNow);

            await act.Should().ThrowAsync<Exception>();
            await serviceBusClient.Received(1).PublishToResultsNotificationTopic(Arg.Is<MatchingResultsNotification>(n => n.WasSuccessful));
            await failureNotificationSender.DidNotReceiveWithAnyArgs().SendFailureNotification(default, default, default);
            // The results were sent, so the attempt has a results sent time, and it failed.
            await searchTrackingEventPublisher.Received(1).ProcessCompleted(
                Arg.Is<(string, DateTime?, int?, MatchingAlgorithmFailureInfo, MatchingAlgorithmRepeatSearchResultsDetails, int?)>(e =>
                    e.Item4.Type == MatchingAlgorithmFailureType.UnexpectedError && e.Item2.HasValue));
        }

        [Test]
        public async Task RunSearch_WhenRepeatSearchIsRecorded_StopsWithoutRunningTrackingOrNotifying()
        {
            historyRepository.IsRepeatSearchRecorded(repeatSearchId).Returns(true);

            await repeatSearchRunner.RunSearch(BuildRequest(), 2, DateTimeOffset.UtcNow);

            await repeatSearchValidator.DidNotReceiveWithAnyArgs().IsOriginalSearchResultSetStored(default);
            await searchTrackingEventPublisher.DidNotReceiveWithAnyArgs().ProcessInitiation(default, default);
            await searchTrackingEventPublisher.DidNotReceiveWithAnyArgs().ProcessCompleted(default);
            await searchService.DidNotReceiveWithAnyArgs().Search(default, default);
            await serviceBusClient.DidNotReceiveWithAnyArgs().PublishToResultsNotificationTopic(default);
            await serviceBusClient.DidNotReceiveWithAnyArgs().PublishToRepeatSearchRequestsTopic(default, default);
            await failureNotificationSender.DidNotReceiveWithAnyArgs().SendFailureNotification(default, default, default);
            await resultSetTracker.DidNotReceiveWithAnyArgs().ApplySearchResultDiff(default, default);
        }

        [Test]
        public async Task RunSearch_WhenAnotherDeliveryRecordsRepeatSearchDuringThisRun_SendsNoResults()
        {
            // The check at the start finds no record. The check after the diff finds the record of a delivery that committed meanwhile.
            historyRepository.IsRepeatSearchRecorded(repeatSearchId).Returns(false, true);

            await repeatSearchRunner.RunSearch(BuildRequest(), 1, DateTimeOffset.UtcNow);

            await resultsBlobStorageClient.DidNotReceive()
                .UploadResults(Arg.Any<ResultSet<MatchingAlgorithmResult>>(), Arg.Any<int>(), Arg.Any<string>());
            await serviceBusClient.DidNotReceiveWithAnyArgs().PublishToResultsNotificationTopic(default);
            await resultSetTracker.DidNotReceiveWithAnyArgs().ApplySearchResultDiff(default, default);
            await failureNotificationSender.DidNotReceiveWithAnyArgs().SendFailureNotification(default, default, default);
            // The completed event of the delivery that sent the results stays on the search tracking row.
            await searchTrackingEventPublisher.DidNotReceiveWithAnyArgs().ProcessCompleted(default);
        }

        [Test]
        public async Task RunSearch_WhenAnotherDeliveryRecordedRepeatSearch_CompletesWithoutFailure()
        {
            resultSetTracker.ApplySearchResultDiff(default, default).ReturnsForAnyArgs(false);

            var act = () => repeatSearchRunner.RunSearch(BuildRequest(), 1, DateTimeOffset.UtcNow);

            await act.Should().NotThrowAsync();
            await failureNotificationSender.DidNotReceiveWithAnyArgs().SendFailureNotification(default, default, default);
            await searchTrackingEventPublisher.Received(1).ProcessCompleted(
                Arg.Is<(string, DateTime?, int?, MatchingAlgorithmFailureInfo, MatchingAlgorithmRepeatSearchResultsDetails, int?)>(e =>
                    e.Item4 == null && e.Item2.HasValue));
        }

        [Test]
        public async Task RunSearch_WhenUploadFailsOnFirstAttempt_RetryReportsSameRemovedDonors()
        {
            var example = SetUpExampleOnRealDiffCalculatorAndTracker();
            example.FailNextUpload = true;
            var request = BuildRequest();

            var firstAttempt = () => repeatSearchRunner.RunSearch(request, 1, DateTimeOffset.UtcNow);
            await firstAttempt.Should().ThrowAsync<Exception>();
            await repeatSearchRunner.RunSearch(request, 2, DateTimeOffset.UtcNow);

            example.UploadedNoLongerMatchingDonors.Should().HaveCount(2).And.AllSatisfy(removed =>
                removed.Should().BeEquivalentTo(new[] { example.NoLongerMatchingDonor, example.DeletedDonor }));
            example.CanonicalSet.Should().BeEquivalentTo(new[] { example.NewDonor, example.UpdatedDonor, example.UnchangedDonor });
            example.AppliedHistoryRecords.Should().ContainSingle()
                .Which.Should().BeEquivalentTo(new { AddedResultCount = 1, UpdatedResultCount = 1, RemovedResultCount = 2 });
        }

        [Test]
        public async Task RunSearch_WhenSetChangeFailsOnFirstAttempt_RetrySendsSameListsAndChangesSetOnce()
        {
            var example = SetUpExampleOnRealDiffCalculatorAndTracker();
            example.FailNextSetChange = true;
            var request = BuildRequest();

            var firstAttempt = () => repeatSearchRunner.RunSearch(request, 1, DateTimeOffset.UtcNow);
            await firstAttempt.Should().ThrowAsync<Exception>();
            await repeatSearchRunner.RunSearch(request, 2, DateTimeOffset.UtcNow);

            example.UploadedNoLongerMatchingDonors.Should().HaveCount(2).And.AllSatisfy(removed =>
                removed.Should().BeEquivalentTo(new[] { example.NoLongerMatchingDonor, example.DeletedDonor }));
            await serviceBusClient.Received(2).PublishToResultsNotificationTopic(Arg.Is<MatchingResultsNotification>(n => n.WasSuccessful));
            example.CanonicalSet.Should().BeEquivalentTo(new[] { example.NewDonor, example.UpdatedDonor, example.UnchangedDonor });
            example.AppliedHistoryRecords.Should().ContainSingle();
        }

        [Test]
        public async Task RunSearch_WhenDeliveredAgainAfterSetChange_DoesNotSendResultsAgain()
        {
            var example = SetUpExampleOnRealDiffCalculatorAndTracker();
            // The search tracking event in `finally` fails after the set change, so the message is delivered again.
            searchTrackingEventPublisher.ProcessCompleted(default)
                .ReturnsForAnyArgs(Task.FromException(new Exception(fixture.Create<string>())), Task.CompletedTask);
            var request = BuildRequest();

            var firstAttempt = () => repeatSearchRunner.RunSearch(request, 1, DateTimeOffset.UtcNow);
            await firstAttempt.Should().ThrowAsync<Exception>();
            await repeatSearchRunner.RunSearch(request, 2, DateTimeOffset.UtcNow);

            example.UploadedNoLongerMatchingDonors.Should().ContainSingle()
                .Which.Should().BeEquivalentTo(new[] { example.NoLongerMatchingDonor, example.DeletedDonor });
            await serviceBusClient.Received(1).PublishToResultsNotificationTopic(Arg.Any<MatchingResultsNotification>());
            example.AppliedHistoryRecords.Should().ContainSingle();
        }

        private void AssertInitiationPublishedBeforeCompletedWithFailure(MatchingAlgorithmFailureType expectedFailureType)
        {
            Received.InOrder(() =>
            {
                searchTrackingEventPublisher.ProcessInitiation(Arg.Any<DateTime>(), Arg.Any<DateTime>());
                // A failure notification was published, so the results sent time is set (not left null or at its default value).
                searchTrackingEventPublisher.ProcessCompleted(
                    Arg.Is<(string, DateTime?, int?, MatchingAlgorithmFailureInfo, MatchingAlgorithmRepeatSearchResultsDetails, int?)>(e =>
                        e.Item4.Type == expectedFailureType && e.Item2.HasValue && e.Item2.Value != default));
            });
        }

        private IdentifiedRepeatSearchRequest BuildRequest() => new()
        {
            OriginalSearchId = originalSearchId,
            RepeatSearchId = repeatSearchId,
            RepeatSearchRequest = new RepeatSearchRequest
            {
                OriginalSearchId = originalSearchId,
                SearchCutoffDate = DateTimeOffset.UtcNow,
                SearchRequest = new SearchRequestBuilder().Build()
            }
        };

        /// <summary>
        /// Builds the runner with the real diff calculator and set tracker, on a canonical set held in a list, so that a test can run
        /// several attempts of one repeat search. The set holds the updated, no longer matching, deleted and unchanged donors.
        /// Since the cutoff: the new donor was added and matches, the updated donor changed and still matches, the no longer matching
        /// donor changed and does not match, the deleted donor left the donor store, and the unchanged donor did not change, so matching
        /// with the cutoff does not return it.
        /// </summary>
        private RepeatSearchExample SetUpExampleOnRealDiffCalculatorAndTracker()
        {
            var donorCodes = fixture.CreateMany<string>(5).ToList();
            var example = new RepeatSearchExample
            {
                NewDonor = donorCodes[0],
                UpdatedDonor = donorCodes[1],
                NoLongerMatchingDonor = donorCodes[2],
                DeletedDonor = donorCodes[3],
                UnchangedDonor = donorCodes[4]
            };
            example.CanonicalSet.AddRange(new[] { example.UpdatedDonor, example.NoLongerMatchingDonor, example.DeletedDonor, example.UnchangedDonor });

            var canonicalResultSetRepository = Substitute.For<ICanonicalResultSetRepository>();
            canonicalResultSetRepository.GetCanonicalResults(default).ReturnsForAnyArgs(_ =>
                (ICollection<SearchResult>)example.CanonicalSet.Select(code => new SearchResult { ExternalDonorCode = code }).ToList());
            canonicalResultSetRepository.ApplyDiffAndRecordRepeatSearch(default, default, default).ReturnsForAnyArgs(call => example.ApplyDiff(
                call.ArgAt<RepeatSearchHistoryRecord>(0), call.ArgAt<IReadOnlyCollection<string>>(1), call.ArgAt<IReadOnlyCollection<string>>(2)));
            historyRepository.IsRepeatSearchRecorded(default)
                .ReturnsForAnyArgs(call => example.RecordedRepeatSearchIds.Contains(call.ArgAt<string>(0)));

            var atlasDonorIds = fixture.CreateMany<int>(3).ToList();
            var donorReader = Substitute.For<IDonorReader>();
            donorReader.GetDonorIdsUpdatedSince(default).ReturnsForAnyArgs((IReadOnlyDictionary<string, int>)new Dictionary<string, int>
            {
                { example.NewDonor, atlasDonorIds[0] },
                { example.UpdatedDonor, atlasDonorIds[1] },
                { example.NoLongerMatchingDonor, atlasDonorIds[2] }
            });
            donorReader.GetExistingExternalDonorCodes(default).ReturnsForAnyArgs(call =>
                (IReadOnlyCollection<string>)call.ArgAt<IEnumerable<string>>(0).Where(code => code != example.DeletedDonor).ToList());

            searchService.Search(default, default).ReturnsForAnyArgs(Task.FromResult<IEnumerable<MatchingAlgorithmResult>>(
                new List<MatchingAlgorithmResult>
                {
                    new() { DonorCode = example.NewDonor, AtlasDonorId = atlasDonorIds[0] },
                    new() { DonorCode = example.UpdatedDonor, AtlasDonorId = atlasDonorIds[1] }
                }));

            resultsBlobStorageClient.UploadResults(Arg.Any<ResultSet<MatchingAlgorithmResult>>(), Arg.Any<int>(), Arg.Any<string>())
                .Returns(call => example.Upload((RepeatMatchingAlgorithmResultSet)call.ArgAt<ResultSet<MatchingAlgorithmResult>>(0)));

            differentialCalculator = new RepeatSearchDifferentialCalculator(donorReader, canonicalResultSetRepository);
            resultSetTracker = new OriginalSearchResultSetTracker(
                Substitute.For<IBlobDownloader>(),
                canonicalResultSetRepository,
                new StoreOriginalSearchResultsBulkCopySettings(),
                Substitute.For<IAtlasLogger>());
            repeatSearchRunner = BuildRunner(DefaultSettings());

            return example;
        }

        /// <summary>
        /// The state of one repeat search across its attempts: the canonical set, the history records and the uploaded results.
        /// </summary>
        private sealed class RepeatSearchExample
        {
            public string NewDonor { get; init; }
            public string UpdatedDonor { get; init; }
            public string NoLongerMatchingDonor { get; init; }
            public string DeletedDonor { get; init; }
            public string UnchangedDonor { get; init; }

            public List<string> CanonicalSet { get; } = new();
            public HashSet<string> RecordedRepeatSearchIds { get; } = new();
            public List<RepeatSearchHistoryRecord> AppliedHistoryRecords { get; } = new();
            public List<List<string>> UploadedNoLongerMatchingDonors { get; } = new();

            /// <summary>
            /// When true, the next upload of the results fails.
            /// </summary>
            public bool FailNextUpload { get; set; }

            /// <summary>
            /// When true, the next set change fails, as a SQL error does: the transaction is rolled back, so nothing changes.
            /// </summary>
            public bool FailNextSetChange { get; set; }

            public Task Upload(RepeatMatchingAlgorithmResultSet resultSet)
            {
                UploadedNoLongerMatchingDonors.Add(resultSet.NoLongerMatchingDonors.ToList());

                if (FailNextUpload)
                {
                    FailNextUpload = false;
                    return Task.FromException(new TimeoutException());
                }

                return Task.CompletedTask;
            }

            /// <summary>
            /// Does what <see cref="ICanonicalResultSetRepository.ApplyDiffAndRecordRepeatSearch"/> does in one transaction.
            /// </summary>
            public Task<bool> ApplyDiff(
                RepeatSearchHistoryRecord historyRecord,
                IReadOnlyCollection<string> donorCodesToAdd,
                IReadOnlyCollection<string> donorCodesToRemove)
            {
                if (FailNextSetChange)
                {
                    FailNextSetChange = false;
                    return Task.FromException<bool>(new TimeoutException());
                }

                if (!RecordedRepeatSearchIds.Add(historyRecord.RepeatSearchRequestId))
                {
                    return Task.FromResult(false);
                }

                CanonicalSet.AddRange(donorCodesToAdd);
                CanonicalSet.RemoveAll(donorCodesToRemove.Contains);
                AppliedHistoryRecords.Add(historyRecord);
                return Task.FromResult(true);
            }
        }
    }
}
