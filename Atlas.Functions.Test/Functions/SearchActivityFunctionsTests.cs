using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.ServiceBus;
using Atlas.Functions.DurableFunctions.Search.Activity;
using Atlas.Functions.Models;
using Atlas.Functions.Services;
using Atlas.Functions.Services.BlobStorageClients;
using Atlas.Functions.Settings;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.SearchTracking.Common.Dispatchers;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.Functions.Test.Functions;

[TestFixture]
internal class SearchActivityFunctionsTests
{
    private const int ParallelBatchSize = 500;

    private IMatchPredictionAlgorithm matchPredictionAlgorithm;
    private IMatchPredictionInputBuilder matchPredictionInputBuilder;
    private ISearchCompletionMessageSender searchCompletionMessageSender;
    private IMatchingResultsDownloader matchingResultsDownloader;
    private ISearchResultsBlobStorageClient searchResultsBlobUploader;
    private IResultsCombiner resultsCombiner;
    private ISearchLogger<SearchLoggingContext> logger;
    private IMatchPredictionRequestBlobClient matchPredictionRequestBlobClient;
    private IMatchPredictionSearchTrackingDispatcher matchPredictionSearchTrackingDispatcher;
    private IMessageBatchPublisher<ParallelMatchPredictionBatchRequest> parallelBatchPublisher;
    private IParallelMatchPredictionRepository parallelMatchPredictionRepository;
    private SearchLoggingContext loggingContext;
    private SearchActivityFunctions functions;

    private Fixture fixture;

    private CreateParallelMatchPredictionRunInfo? capturedRunInfo;
    private List<ParallelMatchPredictionBatchRequest>? publishedRequests;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        capturedRunInfo = null;
        publishedRequests = null;

        matchPredictionAlgorithm = Substitute.For<IMatchPredictionAlgorithm>();
        matchPredictionInputBuilder = Substitute.For<IMatchPredictionInputBuilder>();
        searchCompletionMessageSender = Substitute.For<ISearchCompletionMessageSender>();
        matchingResultsDownloader = Substitute.For<IMatchingResultsDownloader>();
        searchResultsBlobUploader = Substitute.For<ISearchResultsBlobStorageClient>();
        resultsCombiner = Substitute.For<IResultsCombiner>();
        logger = Substitute.For<ISearchLogger<SearchLoggingContext>>();
        matchPredictionRequestBlobClient = Substitute.For<IMatchPredictionRequestBlobClient>();
        matchPredictionSearchTrackingDispatcher = Substitute.For<IMatchPredictionSearchTrackingDispatcher>();
        parallelBatchPublisher = Substitute.For<IMessageBatchPublisher<ParallelMatchPredictionBatchRequest>>();
        parallelMatchPredictionRepository = Substitute.For<IParallelMatchPredictionRepository>();
        loggingContext = new SearchLoggingContext();

        functions = new SearchActivityFunctions(
            matchPredictionAlgorithm,
            matchPredictionInputBuilder,
            searchCompletionMessageSender,
            matchingResultsDownloader,
            searchResultsBlobUploader,
            resultsCombiner,
            logger,
            matchPredictionRequestBlobClient,
            matchPredictionSearchTrackingDispatcher,
            parallelBatchPublisher,
            parallelMatchPredictionRepository,
            Options.Create(new AzureStorageSettings()),
            Options.Create(new OrchestrationSettings { ParallelMatchPredictionBatchSize = ParallelBatchSize }),
            loggingContext);
    }

    // Success path for the non-repeat parallel dispatch: builds inputs at the configured batch size, creates the run
    // record (before publishing), publishes one sequence-aligned request per uploaded blob, and brackets the work
    // with prepare-batches tracking events. Facets that need a different arrange (repeat search, the batched-results
    // branch) have their own tests below.
    [Test]
    public async Task PrepareAndDispatchParallelMatchPredictionBatches_SuccessPath_CreatesRunThenPublishesAlignedRequestsAndTracksProgress()
    {
        var notification = CreateNotification(isRepeat: false);
        var (parameters, runResult, blobLocations) = ArrangeDispatchPipeline(notification, batchCount: 3);
        var searchIdentifier = new Guid(notification.SearchRequestId);

        await functions.PrepareAndDispatchParallelMatchPredictionBatches(parameters);

        // Inputs built at the configured parallel batch size
        matchPredictionInputBuilder.Received(1).BuildMatchPredictionInputs(Arg.Any<OriginalMatchingAlgorithmResultSet>(), ParallelBatchSize);

        // Run record created (with details from the notification) before any batch requests are published
        Received.InOrder(() =>
        {
            parallelMatchPredictionRepository.CreateRun(Arg.Any<CreateParallelMatchPredictionRunInfo>());
            parallelBatchPublisher.BatchPublish(Arg.Any<IEnumerable<ParallelMatchPredictionBatchRequest>>());
        });

        capturedRunInfo.Should().NotBeNull();
        var runInfo = capturedRunInfo!;
        runInfo.SearchIdentifier.Should().Be(searchIdentifier);
        runInfo.IsRepeatSearch.Should().BeFalse();
        runInfo.RepeatSearchIdentifier.Should().BeNull();
        runInfo.ResultsFileName.Should().Be(notification.ResultsFileName);
        runInfo.ResultsBatched.Should().Be(notification.ResultsBatched);
        runInfo.BatchFolderName.Should().Be(notification.BatchFolderName);
        runInfo.MatchingAlgorithmElapsedTime.Should().Be(notification.ElapsedTime);
        runInfo.SearchInitiatedTimeUtc.Should().Be(parameters.SearchInitiatedTimeUtc);
        runInfo.TotalBatchCount.Should().Be(blobLocations.Count);

        // One request published per uploaded blob, with batch ids aligned to blob sequence
        publishedRequests.Should().HaveCount(3);
        foreach (var (request, index) in publishedRequests!.Select((r, i) => (r, i)))
        {
            request.BlobLocation.Should().Be(blobLocations[index]);
            request.BatchSequenceNumber.Should().Be(index);
            request.BatchId.Should().Be(runResult.BatchIdsBySequence[index]);
            request.ParallelRunId.Should().Be(runResult.RunId);
            request.SearchRequestId.Should().Be(notification.SearchRequestId);
            request.IsRepeatSearch.Should().BeFalse();
            request.RepeatSearchRequestId.Should().BeNull();
        }

        // Prepare-batches tracking bracketing, keyed by the search id (no original for a non-repeat search)
        await matchPredictionSearchTrackingDispatcher.Received(1).ProcessPrepareBatchesStarted(searchIdentifier, null);
        await matchPredictionSearchTrackingDispatcher.Received(1).ProcessPrepareBatchesEnded(searchIdentifier, null);
    }

    [Test]
    public async Task PrepareAndDispatchParallelMatchPredictionBatches_ForRepeatSearch_UsesRepeatSearchIdentifiersThroughout()
    {
        var notification = CreateNotification(isRepeat: true);
        var (parameters, _, _) = ArrangeDispatchPipeline(notification);
        var searchIdentifier = new Guid(notification.SearchRequestId);
        var repeatSearchIdentifier = new Guid(notification.RepeatSearchRequestId);

        await functions.PrepareAndDispatchParallelMatchPredictionBatches(parameters);

        // Tracking events are keyed by the repeat search id, with the original search id as context
        await matchPredictionSearchTrackingDispatcher.Received(1).ProcessPrepareBatchesStarted(repeatSearchIdentifier, searchIdentifier);
        await matchPredictionSearchTrackingDispatcher.Received(1).ProcessPrepareBatchesEnded(repeatSearchIdentifier, searchIdentifier);

        capturedRunInfo!.IsRepeatSearch.Should().BeTrue();
        capturedRunInfo.RepeatSearchIdentifier.Should().Be(repeatSearchIdentifier);

        publishedRequests.Should().OnlyContain(r => r.IsRepeatSearch && r.RepeatSearchRequestId == notification.RepeatSearchRequestId);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task PrepareAndDispatchParallelMatchPredictionBatches_DownloadsResultsFromBatchFolderOnlyWhenResultsAreBatched(bool resultsBatched)
    {
        var notification = CreateNotification(isRepeat: false, resultsBatched: resultsBatched);
        var (parameters, _, _) = ArrangeDispatchPipeline(notification);

        await functions.PrepareAndDispatchParallelMatchPredictionBatches(parameters);

        var expectedBatchFolder = resultsBatched ? notification.BatchFolderName : null;
        await matchingResultsDownloader.Received(1).Download(notification.ResultsFileName, false, expectedBatchFolder);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task SendFailureNotification_PublishesFailureMessageAndTracksResultsSent(bool isRepeatSearch)
    {
        var searchIdentifier = fixture.Create<Guid>();
        var repeatSearchIdentifier = isRepeatSearch ? fixture.Create<Guid>() : (Guid?)null;
        var parameters = fixture.Build<SendFailureNotificationParameters>()
            .With(p => p.SearchRequestId, searchIdentifier.ToString())
            .With(p => p.RepeatSearchRequestId, repeatSearchIdentifier?.ToString())
            .Create();

        await functions.SendFailureNotification(parameters);

        await searchCompletionMessageSender.Received(1).PublishFailureMessage(parameters);
        var expectedTrackingIdentifier = repeatSearchIdentifier ?? searchIdentifier;
        var expectedOriginalIdentifier = isRepeatSearch ? searchIdentifier : (Guid?)null;
        await matchPredictionSearchTrackingDispatcher.Received(1).ProcessResultsSent(expectedTrackingIdentifier, expectedOriginalIdentifier);
    }

    [Test]
    public async Task SendMatchPredictionProcessInitiated_ForwardsAllDetailsToTrackingDispatcher()
    {
        var parameters = fixture.Create<MatchPredictionProcessInitiatedParameters>();

        await functions.SendMatchPredictionProcessInitiated(parameters);

        await matchPredictionSearchTrackingDispatcher.Received(1).ProcessInitiation(
            parameters.SearchIdentifier, parameters.OriginalSearchIdentifier, parameters.InitiationTimeUtc, parameters.IsParallelMatchPrediction);
    }

    [Test]
    public async Task SendMatchPredictionBatchProcessingStarted_ForwardsIdentifiersToTrackingDispatcher()
    {
        var parameters = fixture.Create<MatchPredictionSearchIdentifiers>();

        await functions.SendMatchPredictionBatchProcessingStarted(parameters);

        await matchPredictionSearchTrackingDispatcher.Received(1)
            .ProcessRunningBatchesStarted(parameters.SearchIdentifier, parameters.OriginalSearchIdentifier);
    }

    [Test]
    public async Task SendMatchPredictionBatchProcessingEnded_ForwardsIdentifiersToTrackingDispatcher()
    {
        var parameters = fixture.Create<MatchPredictionSearchIdentifiers>();

        await functions.SendMatchPredictionBatchProcessingEnded(parameters);

        await matchPredictionSearchTrackingDispatcher.Received(1)
            .ProcessRunningBatchesEnded(parameters.SearchIdentifier, parameters.OriginalSearchIdentifier);
    }

    [Test]
    public async Task SendMatchPredictionProcessCompleted_ForwardsAllDetailsToTrackingDispatcher()
    {
        var parameters = fixture.Create<MatchPredictionProcessCompletedParameters>();

        await functions.SendMatchPredictionProcessCompleted(parameters);

        await matchPredictionSearchTrackingDispatcher.Received(1).ProcessCompleted(
            (parameters.SearchIdentifier,
                parameters.OriginalSearchIdentifier,
                parameters.IsSuccessful,
                parameters.FailureInfo,
                parameters.DonorsPerBatch,
                parameters.TotalNumberOfBatches));
    }

    private MatchingResultsNotification CreateNotification(bool isRepeat, bool resultsBatched = true) =>
        fixture.Build<MatchingResultsNotification>()
            .With(n => n.SearchRequestId, fixture.Create<Guid>().ToString())
            .With(n => n.RepeatSearchRequestId, isRepeat ? fixture.Create<Guid>().ToString() : null)
            .With(n => n.ResultsBatched, resultsBatched)
            .Without(n => n.SearchRequest)
            .Create();

    /// <summary>
    /// Stubs the full download → build inputs → upload blobs → create run pipeline for
    /// <see cref="SearchActivityFunctions.PrepareAndDispatchParallelMatchPredictionBatches"/>, capturing the run info
    /// passed to the repository (<see cref="capturedRunInfo"/>) and the batch requests published
    /// (<see cref="publishedRequests"/>).
    /// </summary>
    private (PrepareAndDispatchParallelMatchPredictionBatchesParameters Parameters, CreateParallelMatchPredictionRunResult RunResult, List<string> BlobLocations)
        ArrangeDispatchPipeline(MatchingResultsNotification notification, int batchCount = 3)
    {
        var parameters = new PrepareAndDispatchParallelMatchPredictionBatchesParameters
        {
            MatchingResultsNotification = notification,
            SearchInitiatedTimeUtc = fixture.Create<DateTime>()
        };

        var matchingResults = new OriginalMatchingAlgorithmResultSet();
        matchingResultsDownloader.Download(notification.ResultsFileName, notification.IsRepeatSearch, Arg.Any<string>())
            .Returns(matchingResults);

        var inputs = Enumerable.Range(0, batchCount).Select(_ => new MultipleDonorMatchProbabilityInput()).ToList();
        matchPredictionInputBuilder.BuildMatchPredictionInputs(matchingResults, ParallelBatchSize).Returns(inputs);

        var blobLocations = fixture.CreateMany<string>(batchCount).ToList();
        matchPredictionRequestBlobClient.UploadMatchProbabilityRequests(notification.SearchRequestId, inputs)
            .Returns(blobLocations);

        var runResult = new CreateParallelMatchPredictionRunResult(fixture.Create<int>(), fixture.CreateMany<int>(batchCount).ToList());
        parallelMatchPredictionRepository.CreateRun(Arg.Do<CreateParallelMatchPredictionRunInfo>(info => capturedRunInfo = info))
            .Returns(runResult);

        parallelBatchPublisher
            .When(p => p.BatchPublish(Arg.Any<IEnumerable<ParallelMatchPredictionBatchRequest>>()))
            .Do(call => publishedRequests = call.Arg<IEnumerable<ParallelMatchPredictionBatchRequest>>().ToList());

        return (parameters, runResult, blobLocations);
    }
}
