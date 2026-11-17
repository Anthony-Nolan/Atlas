using System;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Common.AzureStorage.Blob;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchRequests;
using Atlas.RepeatSearch.Clients;
using Atlas.RepeatSearch.Data.Repositories;
using Atlas.RepeatSearch.Models;
using Atlas.RepeatSearch.Services.ResultSetTracking;
using Atlas.RepeatSearch.Services.Search;
using Atlas.RepeatSearch.Settings;
using Atlas.RepeatSearch.Settings.Azure;
using Atlas.RepeatSearch.Settings.ServiceBus;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;
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
        private const string OriginalSearchId = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
        private const string RepeatSearchId = "11111111-2222-3333-4444-555555555555";
        private const int WaitIntervalSeconds = 300;
        private const int MaxWaitMinutes = 1440;

        private ISearchService searchService;
        private IRepeatSearchValidator repeatSearchValidator;
        private ISearchTrackingEventPublisher searchTrackingEventPublisher;
        private IRepeatSearchMatchingFailureNotificationSender failureNotificationSender;
        private IRepeatSearchServiceBusClient serviceBusClient;

        private IRepeatSearchRunner repeatSearchRunner;

        [SetUp]
        public void SetUp()
        {
            searchService = Substitute.For<ISearchService>();
            repeatSearchValidator = Substitute.For<IRepeatSearchValidator>();
            searchTrackingEventPublisher = Substitute.For<ISearchTrackingEventPublisher>();
            failureNotificationSender = Substitute.For<IRepeatSearchMatchingFailureNotificationSender>();
            serviceBusClient = Substitute.For<IRepeatSearchServiceBusClient>();

            repeatSearchValidator.IsOriginalSearchResultSetStored(default).ReturnsForAnyArgs(true);

            repeatSearchRunner = BuildRunner(new RepeatSearchSettings
            {
                OriginalResultsWaitIntervalSeconds = WaitIntervalSeconds,
                MaxWaitForOriginalResultsMinutes = MaxWaitMinutes
            });
        }

        private IRepeatSearchRunner BuildRunner(RepeatSearchSettings repeatSearchSettings)
        {
            return new RepeatSearchRunner(
                serviceBusClient,
                searchService,
                Substitute.For<ISearchResultsBlobStorageClient>(),
                Substitute.For<IMatchingAlgorithmSearchLogger>(),
                new MatchingAlgorithmSearchLoggingContext(),
                Substitute.For<IActiveHlaNomenclatureVersionAccessor>(),
                Substitute.For<IActiveDataRefreshRecordAccessor>(),
                Substitute.For<IRepeatSearchHistoryRepository>(),
                repeatSearchValidator,
                Substitute.For<IRepeatSearchDifferentialCalculator>(),
                Substitute.For<IOriginalSearchResultSetTracker>(),
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
                Arg.Is<IdentifiedRepeatSearchRequest>(r => r.RepeatSearchId == RepeatSearchId && r.FirstRequestTimeUtc == enqueuedTime),
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

        private static IdentifiedRepeatSearchRequest BuildRequest() => new()
        {
            OriginalSearchId = OriginalSearchId,
            RepeatSearchId = RepeatSearchId,
            RepeatSearchRequest = new RepeatSearchRequest
            {
                OriginalSearchId = OriginalSearchId,
                SearchCutoffDate = DateTimeOffset.UtcNow,
                SearchRequest = new SearchRequestBuilder().Build()
            }
        };
    }
}
