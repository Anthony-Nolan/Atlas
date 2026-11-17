using Atlas.Common.ApplicationInsights;
using Atlas.SearchTracking.Common.Clients;
using Atlas.SearchTracking.Common.Dispatchers;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;
using AutoFixture;
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.SearchTracking.Test.Dispatchers;

[TestFixture]
public class MatchPredictionSearchTrackingDispatcherTests
{
    /// <summary>Calls one step of the dispatcher for a search and its original search.</summary>
    public delegate Task ProcessStep(
        MatchPredictionSearchTrackingDispatcher dispatcher, Fixture fixture, Guid searchIdentifier, Guid originalSearchIdentifier);

    private Fixture fixture = null!;
    private ISearchTrackingServiceBusClient searchTrackingServiceBusClient = null!;
    private IAtlasLogger logger = null!;
    private Dictionary<string, string>? sentProperties;

    private MatchPredictionSearchTrackingDispatcher dispatcher = null!;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        searchTrackingServiceBusClient = Substitute.For<ISearchTrackingServiceBusClient>();
        logger = Substitute.For<IAtlasLogger>();

        sentProperties = null;
        logger.When(l => l.SendEvent(
                Arg.Any<string>(), Arg.Any<LogLevel>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<Dictionary<string, double>>()))
            .Do(call => sentProperties = call.ArgAt<Dictionary<string, string>>(2));

        dispatcher = new MatchPredictionSearchTrackingDispatcher(searchTrackingServiceBusClient, logger);
    }

    private static IEnumerable<TestCaseData> Steps()
    {
        yield return Step(SearchTrackingEventType.MatchPredictionStarted, (dispatcher, fixture, id, originalId) =>
            dispatcher.ProcessInitiation(id, originalId, fixture.Create<DateTime>(), fixture.Create<bool>(), fixture.Create<bool>()));
        yield return Step(SearchTrackingEventType.MatchPredictionBatchPreparationStarted, (dispatcher, _, id, originalId) =>
            dispatcher.ProcessPrepareBatchesStarted(id, originalId));
        yield return Step(SearchTrackingEventType.MatchPredictionBatchPreparationEnded, (dispatcher, _, id, originalId) =>
            dispatcher.ProcessPrepareBatchesEnded(id, originalId));
        yield return Step(SearchTrackingEventType.MatchPredictionRunningBatchesStarted, (dispatcher, _, id, originalId) =>
            dispatcher.ProcessRunningBatchesStarted(id, originalId));
        yield return Step(SearchTrackingEventType.MatchPredictionRunningBatchesEnded, (dispatcher, _, id, originalId) =>
            dispatcher.ProcessRunningBatchesEnded(id, originalId));
        yield return Step(SearchTrackingEventType.MatchPredictionPersistingResultsStarted, (dispatcher, _, id, originalId) =>
            dispatcher.ProcessPersistingResultsStarted(id, originalId));
        yield return Step(SearchTrackingEventType.MatchPredictionPersistingResultsEnded, (dispatcher, _, id, originalId) =>
            dispatcher.ProcessPersistingResultsEnded(id, originalId));
        yield return Step(SearchTrackingEventType.MatchPredictionCompleted, (dispatcher, fixture, id, originalId) =>
            dispatcher.ProcessCompleted((id, originalId, IsSuccessful: true, FailureInfo: null!, fixture.Create<int>(), fixture.Create<int>())));
        yield return Step(SearchTrackingEventType.MatchPredictionResultsSent, (dispatcher, _, id, originalId) =>
            dispatcher.ProcessResultsSent(id, originalId));

        static TestCaseData Step(SearchTrackingEventType eventType, ProcessStep process) =>
            new TestCaseData(process, eventType).SetArgDisplayNames(eventType.ToString());
    }

    [TestCaseSource(nameof(Steps))]
    public async Task ProcessStep_SendsItsEventToApplicationInsights(ProcessStep process, SearchTrackingEventType eventType)
    {
        var searchIdentifier = fixture.Create<Guid>();
        var originalSearchIdentifier = fixture.Create<Guid>();

        await process(dispatcher, fixture, searchIdentifier, originalSearchIdentifier);

        logger.Received(1).SendEvent(
            eventType.ToString(), LogLevel.Info, Arg.Any<Dictionary<string, string>>(), Arg.Any<Dictionary<string, double>>());
        sentProperties.Should().Contain(nameof(ISearchTrackingEvent.SearchIdentifier), searchIdentifier.ToString())
            .And.Contain(nameof(ISearchTrackingEvent.OriginalSearchIdentifier), originalSearchIdentifier.ToString());
    }

    [TestCaseSource(nameof(Steps))]
    public async Task ProcessStep_PublishesItsEventToTheServiceBus(ProcessStep process, SearchTrackingEventType eventType)
    {
        var searchIdentifier = fixture.Create<Guid>();

        await process(dispatcher, fixture, searchIdentifier, fixture.Create<Guid>());

        // The client method is generic, and the type argument is the class of the event, so the call is read back as it was received.
        var arguments = searchTrackingServiceBusClient.ReceivedCalls().Should().ContainSingle().Which.GetArguments();
        arguments[0].Should().BeAssignableTo<ISearchTrackingEvent>().Which.SearchIdentifier.Should().Be(searchIdentifier);
        arguments[1].Should().Be(eventType);
    }

    [Test]
    public async Task ProcessInitiation_SendsTheParallelAndPrecomputedFlags(
        [Values] bool isParallelMatchPrediction, [Values] bool isPrecomputedMatchPrediction)
    {
        await dispatcher.ProcessInitiation(
            fixture.Create<Guid>(), fixture.Create<Guid>(), fixture.Create<DateTime>(), isParallelMatchPrediction, isPrecomputedMatchPrediction);

        sentProperties.Should()
            .Contain(
                nameof(MatchPredictionStartedEvent.IsParallelMatchPrediction), isParallelMatchPrediction.ToString().ToLowerInvariant())
            .And.Contain(
                nameof(MatchPredictionStartedEvent.IsPrecomputedMatchPrediction), isPrecomputedMatchPrediction.ToString().ToLowerInvariant());
    }

    [Test]
    public async Task ProcessCompleted_WhenMatchPredictionFailed_SendsFailureTypeWithoutMessageOrStacktrace()
    {
        var failureInfo = fixture.Create<MatchPredictionFailureInfo>();

        await dispatcher.ProcessCompleted((fixture.Create<Guid>(), fixture.Create<Guid>(), IsSuccessful: false, failureInfo, null, null));

        sentProperties.Should().Contain("CompletionDetails.IsSuccessful", "false")
            .And.Contain("CompletionDetails.FailureInfo.Type", failureInfo.Type!.Value.ToString())
            .And.NotContainKeys("CompletionDetails.FailureInfo.Message", "CompletionDetails.FailureInfo.ExceptionStacktrace");
    }

    [Test]
    public async Task ProcessCompleted_WhenMatchPredictionFailed_KeepsMessageAndStacktraceInTheServiceBusEvent()
    {
        var failureInfo = fixture.Create<MatchPredictionFailureInfo>();
        var message = failureInfo.Message;
        var exceptionStacktrace = failureInfo.ExceptionStacktrace;

        await dispatcher.ProcessCompleted((fixture.Create<Guid>(), fixture.Create<Guid>(), IsSuccessful: false, failureInfo, null, null));

        await searchTrackingServiceBusClient.Received(1).PublishSearchTrackingEvent(
            Arg.Is<MatchPredictionCompletedEvent>(e =>
                e.CompletionDetails.FailureInfo!.Message == message && e.CompletionDetails.FailureInfo.ExceptionStacktrace == exceptionStacktrace),
            SearchTrackingEventType.MatchPredictionCompleted);
    }

    [Test]
    public async Task ProcessCompleted_WhenMatchPredictionSucceeded_SendsBatchDetailsWithoutFailureInfo()
    {
        var donorsPerBatch = fixture.Create<int>();
        var totalNumberOfBatches = fixture.Create<int>();

        await dispatcher.ProcessCompleted(
            (fixture.Create<Guid>(), fixture.Create<Guid>(), IsSuccessful: true, FailureInfo: null!, donorsPerBatch, totalNumberOfBatches));

        sentProperties.Should().Contain("CompletionDetails.IsSuccessful", "true")
            .And.Contain("CompletionDetails.DonorsPerBatch", donorsPerBatch.ToString())
            .And.Contain("CompletionDetails.TotalNumberOfBatches", totalNumberOfBatches.ToString());
        sentProperties!.Keys.Should().NotContain(key => key.StartsWith("CompletionDetails.FailureInfo"));
    }

    [Test]
    public async Task ProcessRunningBatchesStarted_WhenServiceBusPublishFails_StillSendsEventToApplicationInsights()
    {
        searchTrackingServiceBusClient
            .PublishSearchTrackingEvent(Arg.Any<MatchPredictionTimingEvent>(), Arg.Any<SearchTrackingEventType>())
            .ThrowsAsync(fixture.Create<Exception>());

        var act = () => dispatcher.ProcessRunningBatchesStarted(fixture.Create<Guid>(), fixture.Create<Guid>());

        await act.Should().ThrowAsync<Exception>();
        logger.Received(1).SendEvent(
            nameof(SearchTrackingEventType.MatchPredictionRunningBatchesStarted),
            LogLevel.Info,
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<Dictionary<string, double>>());
    }
}
