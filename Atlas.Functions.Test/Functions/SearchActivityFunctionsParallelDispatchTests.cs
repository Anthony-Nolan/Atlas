using Atlas.Client.Models.Search.Results.Matching;
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

/// <summary>
/// Covers the parallel match prediction dispatch behaviour of <see cref="SearchActivityFunctions"/>, in particular
/// the ATL-151 dispatch-failure handling (record failure in the repository and swallow, leaving the finaliser to do
/// the downstream failure processing). Deliberately a separate fixture from the general
/// <c>SearchActivityFunctionsTests</c> coverage (ATL-112) to keep the two change sets conflict-free.
/// </summary>
[TestFixture]
internal class SearchActivityFunctionsParallelDispatchTests
{
    private IMatchPredictionRequestBlobClient matchPredictionRequestBlobClient;
    private IMessageBatchPublisher<ParallelMatchPredictionBatchRequest> parallelBatchPublisher;
    private IParallelMatchPredictionRepository parallelMatchPredictionRepository;
    private SearchActivityFunctions activityFunctions;

    private Fixture fixture;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        matchPredictionRequestBlobClient = Substitute.For<IMatchPredictionRequestBlobClient>();
        parallelBatchPublisher = Substitute.For<IMessageBatchPublisher<ParallelMatchPredictionBatchRequest>>();
        parallelMatchPredictionRepository = Substitute.For<IParallelMatchPredictionRepository>();

        activityFunctions = new SearchActivityFunctions(
            Substitute.For<IMatchPredictionAlgorithm>(),
            Substitute.For<IMatchPredictionInputBuilder>(),
            Substitute.For<ISearchCompletionMessageSender>(),
            Substitute.For<IMatchingResultsDownloader>(),
            Substitute.For<ISearchResultsBlobStorageClient>(),
            Substitute.For<IResultsCombiner>(),
            Substitute.For<ISearchLogger<SearchLoggingContext>>(),
            matchPredictionRequestBlobClient,
            Substitute.For<IMatchPredictionSearchTrackingDispatcher>(),
            parallelBatchPublisher,
            parallelMatchPredictionRepository,
            Options.Create(new AzureStorageSettings()),
            Options.Create(new OrchestrationSettings { ParallelMatchPredictionBatchSize = fixture.Create<int>() }),
            new SearchLoggingContext());
    }

    [Test]
    public async Task PrepareAndDispatchParallelMatchPredictionBatches_PublishesOneRequestPerUploadedBlob()
    {
        var parameters = BuildParameters();
        var blobLocations = fixture.CreateMany<string>(3).ToList();
        var runCreationResult = ArrangeRunCreation(blobLocations);
        IReadOnlyList<ParallelMatchPredictionBatchRequest>? publishedRequests = null;
        await parallelBatchPublisher.BatchPublish(
            Arg.Do<IEnumerable<ParallelMatchPredictionBatchRequest>>(requests => publishedRequests = requests.ToList()));

        await activityFunctions.PrepareAndDispatchParallelMatchPredictionBatches(parameters);

        publishedRequests.Should().HaveCount(blobLocations.Count);
        foreach (var (request, index) in publishedRequests!.Select((request, index) => (request, index)))
        {
            request.BlobLocation.Should().Be(blobLocations[index]);
            request.SearchRequestId.Should().Be(parameters.MatchingResultsNotification.SearchRequestId);
            request.ParallelRunId.Should().Be(runCreationResult.RunId);
            request.BatchId.Should().Be(runCreationResult.BatchIdsBySequence[index]);
            request.BatchSequenceNumber.Should().Be(index);
        }
    }

    [Test]
    public async Task PrepareAndDispatchParallelMatchPredictionBatches_OnSuccessfulDispatch_DoesNotMarkRunAsDispatchFailed()
    {
        var parameters = BuildParameters();
        ArrangeRunCreation(fixture.CreateMany<string>(2).ToList());

        await activityFunctions.PrepareAndDispatchParallelMatchPredictionBatches(parameters);

        await parallelMatchPredictionRepository.DidNotReceiveWithAnyArgs().MarkRunAsDispatchFailed(default, default, default, default);
    }

    [Test]
    public async Task PrepareAndDispatchParallelMatchPredictionBatches_WhenPublishThrows_MarksRunAsDispatchFailedAndDoesNotThrow()
    {
        var parameters = BuildParameters();
        var runCreationResult = ArrangeRunCreation(fixture.CreateMany<string>(2).ToList());
        var publishException = new InvalidOperationException(fixture.Create<string>());
        parallelBatchPublisher.BatchPublish(Arg.Any<IEnumerable<ParallelMatchPredictionBatchRequest>>())
            .Returns(Task.FromException(publishException));

        // The exception is deliberately swallowed after being recorded: the finaliser performs the failure
        // processing, and rethrowing would make the durable framework retry the activity (duplicating the run).
        await activityFunctions.Invoking(a => a.PrepareAndDispatchParallelMatchPredictionBatches(parameters))
            .Should().NotThrowAsync();

        await parallelMatchPredictionRepository.Received(1).MarkRunAsDispatchFailed(
            runCreationResult.RunId,
            Arg.Is<string>(message => message.Contains(publishException.Message)),
            Arg.Is<string>(exception => exception.Contains(publishException.Message)),
            Arg.Any<DateTime>());
    }

    [Test]
    public async Task PrepareAndDispatchParallelMatchPredictionBatches_WhenMarkingDispatchFailureAlsoThrows_ThatExceptionPropagates()
    {
        var parameters = BuildParameters();
        ArrangeRunCreation(fixture.CreateMany<string>(2).ToList());
        parallelBatchPublisher.BatchPublish(Arg.Any<IEnumerable<ParallelMatchPredictionBatchRequest>>())
            .Returns(Task.FromException(new InvalidOperationException(fixture.Create<string>())));
        var markFailureException = new ApplicationException(fixture.Create<string>());
        parallelMatchPredictionRepository.MarkRunAsDispatchFailed(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>())
            .Returns(Task.FromException(markFailureException));

        // The DB write failing means the dispatch failure was NOT recorded — propagate so the durable retry,
        // abandonment sweep and ops alerting safety net take over.
        var thrown = await activityFunctions.Invoking(a => a.PrepareAndDispatchParallelMatchPredictionBatches(parameters))
            .Should().ThrowAsync<ApplicationException>();
        thrown.Which.Should().BeSameAs(markFailureException);
    }

    [Test]
    public async Task PrepareAndDispatchParallelMatchPredictionBatches_WhenCreateRunThrows_ExceptionPropagatesAndDispatchFailureIsNotMarked()
    {
        var parameters = BuildParameters();
        matchPredictionRequestBlobClient.UploadMatchProbabilityRequests(Arg.Any<string>(), Arg.Any<IEnumerable<MultipleDonorMatchProbabilityInput>>())
            .Returns(fixture.CreateMany<string>(2).ToList());
        var createRunException = new InvalidOperationException(fixture.Create<string>());
        parallelMatchPredictionRepository.CreateRun(Arg.Any<CreateParallelMatchPredictionRunInfo>())
            .Returns(Task.FromException<CreateParallelMatchPredictionRunResult>(createRunException));

        // CreateRun failures stay on the existing orchestrator retry/notification path — the dispatch-failure
        // handling only covers publishing, once run and batch rows exist to record the failure against.
        var thrown = await activityFunctions.Invoking(a => a.PrepareAndDispatchParallelMatchPredictionBatches(parameters))
            .Should().ThrowAsync<InvalidOperationException>();
        thrown.Which.Should().BeSameAs(createRunException);

        await parallelBatchPublisher.DidNotReceiveWithAnyArgs().BatchPublish(default);
        await parallelMatchPredictionRepository.DidNotReceiveWithAnyArgs().MarkRunAsDispatchFailed(default, default, default, default);
    }

    [Test]
    public async Task PrepareAndDispatchParallelMatchPredictionBatches_ForRepeatSearch_PublishesRequestsWithRepeatSearchRequestId()
    {
        var parameters = BuildParameters(isRepeatSearch: true);
        ArrangeRunCreation(fixture.CreateMany<string>(2).ToList());
        IReadOnlyList<ParallelMatchPredictionBatchRequest>? publishedRequests = null;
        await parallelBatchPublisher.BatchPublish(
            Arg.Do<IEnumerable<ParallelMatchPredictionBatchRequest>>(requests => publishedRequests = requests.ToList()));

        await activityFunctions.PrepareAndDispatchParallelMatchPredictionBatches(parameters);

        publishedRequests.Should().OnlyContain(request =>
            request.IsRepeatSearch && request.RepeatSearchRequestId == parameters.MatchingResultsNotification.RepeatSearchRequestId);
    }

    /// <summary>
    /// Stubs the blob upload to return the given locations and run creation to return a run with one batch id per
    /// location, mirroring the repository's pre-created-batch contract.
    /// </summary>
    private CreateParallelMatchPredictionRunResult ArrangeRunCreation(IReadOnlyList<string> blobLocations)
    {
        matchPredictionRequestBlobClient.UploadMatchProbabilityRequests(Arg.Any<string>(), Arg.Any<IEnumerable<MultipleDonorMatchProbabilityInput>>())
            .Returns(blobLocations);

        var runCreationResult = new CreateParallelMatchPredictionRunResult(fixture.Create<int>(), fixture.CreateMany<int>(blobLocations.Count).ToList());
        parallelMatchPredictionRepository.CreateRun(Arg.Any<CreateParallelMatchPredictionRunInfo>())
            .Returns(runCreationResult);

        return runCreationResult;
    }

    private PrepareAndDispatchParallelMatchPredictionBatchesParameters BuildParameters(bool isRepeatSearch = false)
    {
        // SearchRequestId / RepeatSearchRequestId must be Guid-parseable — the activity calls new Guid(...) on them.
        var notification = fixture.Build<MatchingResultsNotification>()
            .With(n => n.SearchRequestId, Guid.NewGuid().ToString())
            .With(n => n.RepeatSearchRequestId, isRepeatSearch ? Guid.NewGuid().ToString() : null)
            .Create();

        return new PrepareAndDispatchParallelMatchPredictionBatchesParameters
        {
            MatchingResultsNotification = notification,
            SearchInitiatedTimeUtc = fixture.Create<DateTime>(),
        };
    }
}
