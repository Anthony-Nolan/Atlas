using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Functions.Models;
using Atlas.Functions.Services;
using Atlas.Functions.Services.BlobStorageClients;
using Atlas.Functions.Settings;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.SearchTracking.Common.Dispatchers;
using NSubstitute;
using NUnit.Framework;
using Microsoft.Extensions.Options;

namespace Atlas.Functions.Test.Services
{
    [TestFixture]
    internal class ParallelMatchPredictionCompletionServiceTests
    {
        private IParallelMatchPredictionRepository repository;
        private ISearchCompletionMessageSender searchCompletionMessageSender;
        private ParallelMatchPredictionCompletionService completionService;

        [SetUp]
        public void SetUp()
        {
            repository = Substitute.For<IParallelMatchPredictionRepository>();
            searchCompletionMessageSender = Substitute.For<ISearchCompletionMessageSender>();

            completionService = new ParallelMatchPredictionCompletionService(
                repository,
                Substitute.For<IMatchingResultsDownloader>(),
                Substitute.For<IResultsCombiner>(),
                Substitute.For<ISearchResultsBlobStorageClient>(),
                searchCompletionMessageSender,
                Substitute.For<IMatchPredictionSearchTrackingDispatcher>(),
                Substitute.For<ISearchLogger<SearchLoggingContext>>(),
                Options.Create(new AzureStorageSettings()),
                Options.Create(new OrchestrationSettings())
            );
        }

        [Test]
        public async Task AbandonRun_WhenRunTransitionedToAbandoned_PublishesSingleFailureNotification()
        {
            var searchId = Guid.NewGuid();
            repository.TryMarkRunAsAbandoned(Arg.Any<int>(), Arg.Any<DateTime>())
                .Returns(new AbandonedRunHeader(searchId, null, false));

            await completionService.AbandonRun(42);

            await searchCompletionMessageSender.Received(1).PublishFailureMessage(
                Arg.Is<SendFailureNotificationParameters>(p =>
                    p.SearchRequestId == searchId.ToString()
                    && p.RepeatSearchRequestId == null
                    && p.StageReached == "MatchPredictionBatchProcessing")
            );
        }

        [Test]
        public async Task AbandonRun_RepeatSearch_PublishesNotificationWithRepeatSearchId()
        {
            var searchId = Guid.NewGuid();
            var repeatId = Guid.NewGuid();
            repository.TryMarkRunAsAbandoned(Arg.Any<int>(), Arg.Any<DateTime>())
                .Returns(new AbandonedRunHeader(searchId, repeatId, true));

            await completionService.AbandonRun(42);

            await searchCompletionMessageSender.Received(1).PublishFailureMessage(
                Arg.Is<SendFailureNotificationParameters>(p =>
                    p.SearchRequestId == searchId.ToString()
                    && p.RepeatSearchRequestId == repeatId.ToString())
            );
        }

        [Test]
        public async Task AbandonRun_WhenRunNoLongerRunning_DoesNotPublishNotification()
        {
            repository.TryMarkRunAsAbandoned(Arg.Any<int>(), Arg.Any<DateTime>())
                .Returns((AbandonedRunHeader)null);

            await completionService.AbandonRun(42);

            await searchCompletionMessageSender.DidNotReceiveWithAnyArgs()
                .PublishFailureMessage(Arg.Any<SendFailureNotificationParameters>());
        }
    }
}
