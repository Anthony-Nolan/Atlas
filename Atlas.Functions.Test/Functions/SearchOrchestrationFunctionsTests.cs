using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results.LogFile;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.ApplicationInsights;
using Atlas.Functions.DurableFunctions.Search.Activity;
using Atlas.Functions.DurableFunctions.Search.Orchestration;
using Atlas.Functions.Exceptions;
using Atlas.Functions.Models;
using Atlas.Functions.Settings;
using Atlas.Functions.Test.TestHelpers;
using Atlas.SearchTracking.Common.Enums;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.DurableTask;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.Functions.Test.Functions;

/// <summary>
/// Unit tests for the durable orchestrators, using a mocked <see cref="TaskOrchestrationContext"/>
/// (the officially recommended approach for the isolated worker model). These verify orchestration
/// control flow — which activities are scheduled, with what inputs — not replay/determinism semantics.
/// </summary>
[TestFixture]
internal class SearchOrchestrationFunctionsTests
{
    private const int SequentialProcessingBatchSize = 10;

    private ISearchLogger<SearchLoggingContext> logger;
    private TaskOrchestrationContext context;
    private SearchOrchestrationFunctions functions;
    private DateTime orchestrationStartTime;

    private Fixture fixture;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        logger = Substitute.For<ISearchLogger<SearchLoggingContext>>();
        context = Substitute.For<TaskOrchestrationContext>();
        orchestrationStartTime = fixture.Create<DateTime>();
        context.CurrentUtcDateTime.Returns(orchestrationStartTime);

        functions = new SearchOrchestrationFunctions(
            logger,
            MapperProvider.Mapper,
            Options.Create(new AzureStorageSettings { MatchPredictionProcessingBatchSize = SequentialProcessingBatchSize }),
            new SearchLoggingContext());
    }

    [Test]
    public async Task SearchOrchestrator_WhenMatchingFailed_SendsFailureNotificationWithMatchingFailureInfo_AndStopsOrchestrating()
    {
        var notification = CreateNotification(parallelMatchPrediction: null, wasSuccessful: false);
        SetUpSearchOrchestratorInput(notification);

        var output = await functions.SearchOrchestrator(context);

        output.Should().BeNull();
        await AssertActivityCalled<SendFailureNotificationParameters>(
            nameof(SearchActivityFunctions.SendFailureNotification),
            p => p.StageReached == "Matching Algorithm"
                 && p.SearchRequestId == notification.SearchRequestId
                 && ReferenceEquals(p.MatchingAlgorithmFailureInfo, notification.FailureInfo));
        context.Received(1).SetCustomStatus("Search failed, during stage: Matching Algorithm");
        await AssertActivityNotCalled(nameof(SearchActivityFunctions.SendMatchPredictionProcessInitiated));
    }

    [Test]
    public async Task SearchOrchestrator_ParallelPath_SendsInitiationTrackingWithParallelFlag()
    {
        var notification = CreateNotification(parallelMatchPrediction: true);
        SetUpSearchOrchestratorInput(notification);

        await functions.SearchOrchestrator(context);

        var searchIdentifier = new Guid(notification.SearchRequestId);
        await AssertActivityCalled<MatchPredictionProcessInitiatedParameters>(
            nameof(SearchActivityFunctions.SendMatchPredictionProcessInitiated),
            p => p.IsParallelMatchPrediction
                 && p.SearchIdentifier == searchIdentifier
                 && p.OriginalSearchIdentifier == null
                 && p.InitiationTimeUtc == orchestrationStartTime);
    }

    [Test]
    public async Task SearchOrchestrator_ParallelPath_DispatchesBatchesAndReturnsMatchingDonorCount()
    {
        var notification = CreateNotification(parallelMatchPrediction: true);
        SetUpSearchOrchestratorInput(notification);

        var output = await functions.SearchOrchestrator(context);

        await AssertActivityCalled<PrepareAndDispatchParallelMatchPredictionBatchesParameters>(
            nameof(SearchActivityFunctions.PrepareAndDispatchParallelMatchPredictionBatches),
            p => ReferenceEquals(p.MatchingResultsNotification, notification)
                 && p.SearchInitiatedTimeUtc == orchestrationStartTime);
        output.MatchingDonorCount.Should().Be(notification.NumberOfResults!.Value);
        context.Received(1).SetCustomStatus(Arg.Is<object>(status =>
            status is OrchestrationStatus
            && ((OrchestrationStatus)status).LastCompletedStage == nameof(SearchActivityFunctions.PrepareAndDispatchParallelMatchPredictionBatches)));
    }

    [Test]
    public async Task SearchOrchestrator_ParallelPath_WhenNumberOfResultsIsNull_ReturnsMinusOneDonorCount()
    {
        var notification = CreateNotification(parallelMatchPrediction: true, hasNumberOfResults: false);
        SetUpSearchOrchestratorInput(notification);

        var output = await functions.SearchOrchestrator(context);

        output.MatchingDonorCount.Should().Be(-1);
    }

    [Test]
    public async Task SearchOrchestrator_ParallelPath_DoesNotRunSequentialMatchPredictionOrCompletionSteps()
    {
        var notification = CreateNotification(parallelMatchPrediction: true);
        SetUpSearchOrchestratorInput(notification);

        await functions.SearchOrchestrator(context);

        // The aggregator owns everything after dispatch: no inline match prediction, result persistence,
        // log upload, or completion tracking from the orchestrator
        await context.DidNotReceive().CallActivityAsync<TimedResultSet<IList<string>>>(
            Arg.Any<TaskName>(), Arg.Any<object>(), Arg.Any<TaskOptions>());
        await AssertActivityNotCalled(nameof(SearchActivityFunctions.PersistSearchResults));
        await AssertActivityNotCalled(nameof(SearchActivityFunctions.UploadSearchLog));
        await AssertActivityNotCalled(nameof(SearchActivityFunctions.SendMatchPredictionProcessCompleted));
    }

    [Test]
    public async Task SearchOrchestrator_ParallelPath_WhenDispatchFails_SendsFailureNotificationAndThrowsHandledException()
    {
        var notification = CreateNotification(parallelMatchPrediction: true);
        SetUpSearchOrchestratorInput(notification);
        context.CallActivityAsync(
                Arg.Is<TaskName>(name => name.Name == nameof(SearchActivityFunctions.PrepareAndDispatchParallelMatchPredictionBatches)),
                Arg.Any<object>(),
                Arg.Any<TaskOptions>())
            .Returns(Task.FromException(new InvalidOperationException(fixture.Create<string>())));

        await functions.Invoking(f => f.SearchOrchestrator(context))
            .Should().ThrowAsync<HandledOrchestrationException>();

        await AssertActivityCalled<SendFailureNotificationParameters>(
            nameof(SearchActivityFunctions.SendFailureNotification),
            p => p.StageReached == nameof(SearchActivityFunctions.PrepareAndDispatchParallelMatchPredictionBatches)
                 && p.SearchRequestId == notification.SearchRequestId);
        context.Received(1).SetCustomStatus(
            $"Search failed, during stage: {nameof(SearchActivityFunctions.PrepareAndDispatchParallelMatchPredictionBatches)}");
        await AssertActivityNotCalled(nameof(SearchActivityFunctions.SendMatchPredictionProcessCompleted));
    }

    [TestCase(false)]
    [TestCase(null)]
    public async Task SearchOrchestrator_SequentialPath_SendsCompletionTrackingWithSuccess(bool? parallelMatchPrediction)
    {
        var notification = CreateNotification(parallelMatchPrediction);
        SetUpSearchOrchestratorInput(notification);
        SetUpSuccessfulSequentialMatchPrediction(requestLocationCount: 2);

        var output = await functions.SearchOrchestrator(context);

        output.Should().NotBeNull();
        var searchIdentifier = new Guid(notification.SearchRequestId);
        await AssertActivityCalled<MatchPredictionProcessCompletedParameters>(
            nameof(SearchActivityFunctions.SendMatchPredictionProcessCompleted),
            p => p.IsSuccessful
                 && p.FailureInfo == null
                 && p.SearchIdentifier == searchIdentifier
                 && p.DonorsPerBatch == SequentialProcessingBatchSize
                 && p.TotalNumberOfBatches == 1);
        await AssertActivityCalled<SearchLog>(
            nameof(SearchActivityFunctions.UploadSearchLog),
            log => log.WasSuccessful && log.SearchRequestId == notification.SearchRequestId);
    }

    [Test]
    public async Task SearchOrchestrator_SequentialPath_WhenStageFails_SendsCompletionTrackingWithOrchestrationFailureInfo()
    {
        var notification = CreateNotification(parallelMatchPrediction: false);
        SetUpSearchOrchestratorInput(notification);
        context.CallActivityAsync<TimedResultSet<IList<string>>>(
                Arg.Is<TaskName>(name => name.Name == nameof(SearchActivityFunctions.PrepareMatchPredictionBatches)),
                Arg.Any<object>(),
                Arg.Any<TaskOptions>())
            .Returns(Task.FromException<TimedResultSet<IList<string>>>(new InvalidOperationException(fixture.Create<string>())));

        await functions.Invoking(f => f.SearchOrchestrator(context))
            .Should().ThrowAsync<HandledOrchestrationException>();

        await AssertActivityCalled<SendFailureNotificationParameters>(
            nameof(SearchActivityFunctions.SendFailureNotification),
            p => p.StageReached == nameof(SearchActivityFunctions.PrepareMatchPredictionBatches));
        await AssertActivityCalled<MatchPredictionProcessCompletedParameters>(
            nameof(SearchActivityFunctions.SendMatchPredictionProcessCompleted),
            p => !p.IsSuccessful && p.FailureInfo.Type == MatchPredictionFailureType.OrchestrationError);
        await AssertActivityCalled<SearchLog>(
            nameof(SearchActivityFunctions.UploadSearchLog),
            log => !log.WasSuccessful);
    }

    [Test]
    public async Task RepeatSearchOrchestrator_ParallelPath_TracksWithRepeatSearchIdentifiers_AndDispatchesBatches()
    {
        var notification = CreateNotification(parallelMatchPrediction: true, isRepeatSearch: true);
        context.GetInput<MatchingResultsNotification>().Returns(notification);
        var repeatSearchIdentifier = new Guid(notification.RepeatSearchRequestId);
        var originalSearchIdentifier = new Guid(notification.SearchRequestId);

        var output = await functions.RepeatSearchOrchestrator(context);

        await AssertActivityCalled<MatchPredictionProcessInitiatedParameters>(
            nameof(SearchActivityFunctions.SendMatchPredictionProcessInitiated),
            p => p.IsParallelMatchPrediction
                 && p.SearchIdentifier == repeatSearchIdentifier
                 && p.OriginalSearchIdentifier == originalSearchIdentifier);
        await AssertActivityCalled<PrepareAndDispatchParallelMatchPredictionBatchesParameters>(
            nameof(SearchActivityFunctions.PrepareAndDispatchParallelMatchPredictionBatches),
            p => ReferenceEquals(p.MatchingResultsNotification, notification));
        output.MatchingDonorCount.Should().Be(notification.NumberOfResults!.Value);
        await AssertActivityNotCalled(nameof(SearchActivityFunctions.SendMatchPredictionProcessCompleted));
    }

    [Test]
    public async Task RepeatSearchOrchestrator_WhenMatchingFailed_SendsFailureNotificationWithBothIdentifiers_AndStopsOrchestrating()
    {
        var notification = CreateNotification(parallelMatchPrediction: null, isRepeatSearch: true, wasSuccessful: false);
        context.GetInput<MatchingResultsNotification>().Returns(notification);

        var output = await functions.RepeatSearchOrchestrator(context);

        output.Should().BeNull();
        await AssertActivityCalled<SendFailureNotificationParameters>(
            nameof(SearchActivityFunctions.SendFailureNotification),
            p => p.StageReached == "Matching Algorithm"
                 && p.SearchRequestId == notification.SearchRequestId
                 && p.RepeatSearchRequestId == notification.RepeatSearchRequestId
                 && ReferenceEquals(p.MatchingAlgorithmFailureInfo, notification.FailureInfo));
        await AssertActivityNotCalled(nameof(SearchActivityFunctions.SendMatchPredictionProcessInitiated));
    }

    private MatchingResultsNotification CreateNotification(
        bool? parallelMatchPrediction,
        bool isRepeatSearch = false,
        bool wasSuccessful = true,
        bool hasNumberOfResults = true)
    {
        return fixture.Build<MatchingResultsNotification>()
            .With(n => n.SearchRequestId, Guid.NewGuid().ToString())
            .With(n => n.RepeatSearchRequestId, isRepeatSearch ? Guid.NewGuid().ToString() : null)
            .With(n => n.WasSuccessful, wasSuccessful)
            .With(n => n.NumberOfResults, hasNumberOfResults ? fixture.Create<int>() : (int?)null)
            .With(n => n.SearchRequest, new SearchRequest { ParallelMatchPrediction = parallelMatchPrediction })
            .Create();
    }

    private void SetUpSearchOrchestratorInput(MatchingResultsNotification notification)
    {
        context.GetInput<SearchOrchestratorParameters>().Returns(new SearchOrchestratorParameters
        {
            MatchingResultsNotification = notification,
            InitiationTime = fixture.Create<DateTimeOffset>()
        });
    }

    /// <summary>
    /// Stubs the two typed activity calls the sequential path awaits: batch preparation (returning
    /// <paramref name="requestLocationCount"/> request locations) and one match prediction result
    /// dictionary per location.
    /// </summary>
    private void SetUpSuccessfulSequentialMatchPrediction(int requestLocationCount)
    {
        var requestLocations = fixture.CreateMany<string>(requestLocationCount).ToList();
        context.CallActivityAsync<TimedResultSet<IList<string>>>(
                Arg.Is<TaskName>(name => name.Name == nameof(SearchActivityFunctions.PrepareMatchPredictionBatches)),
                Arg.Any<object>(),
                Arg.Any<TaskOptions>())
            .Returns(new TimedResultSet<IList<string>>
            {
                ResultSet = requestLocations,
                ElapsedTime = fixture.Create<TimeSpan>(),
                FinishedTimeUtc = orchestrationStartTime
            });

        // Distinct dictionaries per call — the orchestrator merges them into one dictionary, so duplicate keys would throw
        var resultLocationsPerBatch = Enumerable.Range(0, requestLocationCount)
            .Select(_ => (IReadOnlyDictionary<int, string>)fixture.Create<Dictionary<int, string>>())
            .ToArray();
        context.CallActivityAsync<IReadOnlyDictionary<int, string>>(
                Arg.Is<TaskName>(name => name.Name == nameof(SearchActivityFunctions.RunMatchPredictionBatch)),
                Arg.Any<object>(),
                Arg.Any<TaskOptions>())
            .Returns(resultLocationsPerBatch.First(), resultLocationsPerBatch.Skip(1).ToArray());
    }

    private async Task AssertActivityCalled<TInput>(string activityName, Func<TInput, bool> inputMatches) =>
        await context.Received(1).CallActivityAsync(
            Arg.Is<TaskName>(name => name.Name == activityName),
            Arg.Is<object>(input => input is TInput && inputMatches((TInput)input)),
            Arg.Any<TaskOptions>());

    private async Task AssertActivityNotCalled(string activityName) =>
        await context.DidNotReceive().CallActivityAsync(
            Arg.Is<TaskName>(name => name.Name == activityName),
            Arg.Any<object>(),
            Arg.Any<TaskOptions>());
}
