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

        private ISearchService searchService;
        private IRepeatSearchValidator repeatSearchValidator;
        private ISearchTrackingEventPublisher searchTrackingEventPublisher;

        private IRepeatSearchRunner repeatSearchRunner;

        [SetUp]
        public void SetUp()
        {
            searchService = Substitute.For<ISearchService>();
            repeatSearchValidator = Substitute.For<IRepeatSearchValidator>();
            searchTrackingEventPublisher = Substitute.For<ISearchTrackingEventPublisher>();

            repeatSearchRunner = new RepeatSearchRunner(
                Substitute.For<IRepeatSearchServiceBusClient>(),
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
                Substitute.For<IRepeatSearchMatchingFailureNotificationSender>(),
                new MatchingAlgorithmSearchTrackingContext(),
                searchTrackingEventPublisher);
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

        private void AssertInitiationPublishedBeforeCompletedWithFailure(MatchingAlgorithmFailureType expectedFailureType)
        {
            Received.InOrder(() =>
            {
                searchTrackingEventPublisher.ProcessInitiation(Arg.Any<DateTime>(), Arg.Any<DateTime>());
                searchTrackingEventPublisher.ProcessCompleted(
                    Arg.Is<(string, DateTime?, int?, MatchingAlgorithmFailureInfo, MatchingAlgorithmRepeatSearchResultsDetails, int?)>(e =>
                        e.Item4.Type == expectedFailureType));
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
