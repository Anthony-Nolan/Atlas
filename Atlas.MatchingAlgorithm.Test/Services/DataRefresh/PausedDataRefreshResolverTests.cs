using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchingAlgorithm.Client.Models.DataRefresh;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Models.DataRefresh;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Notifications;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchingAlgorithm.Test.TestHelpers;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh;

[TestFixture]
public class PausedDataRefreshResolverTests
{
    private Fixture fixture;
    private IDataRefreshHistoryRepository dataRefreshHistoryRepository;
    private IDataRefreshServiceBusClient serviceBusClient;
    private IDataRefreshCompletionNotifier completionNotifier;
    private IDonorGenotypePrecomputationRunCanceller precomputationRunCanceller;
    private IDataRefreshManualDecisionEvents manualDecisionEvents;
    private DateTimeOffset now;
    private DataRefreshRecord pausedRecord;

    private IPausedDataRefreshResolver resolver;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        dataRefreshHistoryRepository = Substitute.For<IDataRefreshHistoryRepository>();
        serviceBusClient = Substitute.For<IDataRefreshServiceBusClient>();
        completionNotifier = Substitute.For<IDataRefreshCompletionNotifier>();
        precomputationRunCanceller = Substitute.For<IDonorGenotypePrecomputationRunCanceller>();
        manualDecisionEvents = Substitute.For<IDataRefreshManualDecisionEvents>();
        now = fixture.Create<DateTimeOffset>();

        pausedRecord = DataRefreshRecordBuilder.New
            .With(r => r.Id, fixture.Create<int>())
            .PausedForManualDecision()
            .Build();
        dataRefreshHistoryRepository.FindRecord(pausedRecord.Id).Returns(pausedRecord);

        // The happy path: the record is paused, so the decision update changes it.
        dataRefreshHistoryRepository.TryTakeManualDecision(default, default, default, default).ReturnsForAnyArgs(true);

        resolver = new PausedDataRefreshResolver(
            dataRefreshHistoryRepository,
            serviceBusClient,
            completionNotifier,
            precomputationRunCanceller,
            manualDecisionEvents,
            new InstantTimeProvider(now),
            NullLogger<PausedDataRefreshResolver>.Instance);
    }

    #region A request that is not valid

    private static IEnumerable<TestCaseData> InvalidRequests()
    {
        yield return new TestCaseData((ResolvePausedDataRefreshRequest)null).SetArgDisplayNames("No body");
        yield return new TestCaseData(new ResolvePausedDataRefreshRequest { DataRefreshRecordId = 0, Decision = DataRefreshManualDecision.Fail })
            .SetArgDisplayNames("Record id 0");
        yield return new TestCaseData(new ResolvePausedDataRefreshRequest { DataRefreshRecordId = 1 }).SetArgDisplayNames("No decision");
        yield return new TestCaseData(new ResolvePausedDataRefreshRequest { DataRefreshRecordId = 1, Decision = (DataRefreshManualDecision)99 })
            .SetArgDisplayNames("A decision that is not defined");
    }

    [TestCaseSource(nameof(InvalidRequests))]
    public async Task Resolve_WithARequestThatIsNotValid_ReturnsInvalidRequest_AndChangesNothing(ResolvePausedDataRefreshRequest request)
    {
        var resolution = await resolver.Resolve(request);

        resolution.Outcome.Should().Be(PausedDataRefreshResolutionOutcome.InvalidRequest);
        await dataRefreshHistoryRepository.DidNotReceiveWithAnyArgs().FindRecord(default);
        await dataRefreshHistoryRepository.DidNotReceiveWithAnyArgs().TryTakeManualDecision(default, default, default, default);
    }

    [Test]
    public async Task Resolve_ForAnUnknownRecord_ReturnsNotFound_AndChangesNothing()
    {
        var request = Request(DataRefreshManualDecision.Complete, fixture.Create<int>());
        dataRefreshHistoryRepository.FindRecord(request.DataRefreshRecordId).Returns((DataRefreshRecord)null);

        var resolution = await resolver.Resolve(request);

        resolution.Outcome.Should().Be(PausedDataRefreshResolutionOutcome.NotFound);
        await dataRefreshHistoryRepository.DidNotReceiveWithAnyArgs().TryTakeManualDecision(default, default, default, default);
    }

    #endregion

    #region A record that is not paused

    [Test]
    public async Task Resolve_WhenTheUpdateChangesNoRow_ReturnsAConflict_AndDoesNothingElse(
        [Values] DataRefreshManualDecision decision)
    {
        dataRefreshHistoryRepository.TryTakeManualDecision(default, default, default, default).ReturnsForAnyArgs(false);

        var resolution = await resolver.Resolve(Request(decision));

        resolution.Outcome.Should().Be(PausedDataRefreshResolutionOutcome.Conflict);
        manualDecisionEvents.DidNotReceiveWithAnyArgs().DecisionTaken(default, default, default, default);
        await serviceBusClient.DidNotReceiveWithAnyArgs().PublishToRequestTopic(default);
        await completionNotifier.DidNotReceiveWithAnyArgs().NotifyOfFailure(default);
        await precomputationRunCanceller.DidNotReceiveWithAnyArgs().CancelRun(default);
    }

    [Test]
    public async Task Resolve_WhenTheRecordIsClosed_ReturnsAConflictThatSaysSo()
    {
        dataRefreshHistoryRepository.TryTakeManualDecision(default, default, default, default).ReturnsForAnyArgs(false);
        dataRefreshHistoryRepository.FindRecord(pausedRecord.Id).Returns(
            pausedRecord,
            DataRefreshRecordBuilder.New.With(r => r.Id, pausedRecord.Id).SuccessfullyCompleted().Build());

        var resolution = await resolver.Resolve(Request(DataRefreshManualDecision.Complete));

        resolution.Message.Should().Contain("it is closed");
    }

    [Test]
    public async Task Resolve_WhenADecisionIsTakenAlready_ReturnsAConflictThatNamesIt()
    {
        dataRefreshHistoryRepository.TryTakeManualDecision(default, default, default, default).ReturnsForAnyArgs(false);
        dataRefreshHistoryRepository.FindRecord(pausedRecord.Id).Returns(
            pausedRecord,
            DataRefreshRecordBuilder.New
                .With(r => r.Id, pausedRecord.Id)
                .WithPendingManualDecision(DataRefreshManualDecision.RetryFailedBatches)
                .Build());

        var resolution = await resolver.Resolve(Request(DataRefreshManualDecision.Complete));

        resolution.Message.Should().Contain($"the decision {DataRefreshManualDecision.RetryFailedBatches} is taken already");
    }

    [Test]
    public async Task Resolve_WhenTheRecordIsNotPausedYet_ReturnsAConflictThatSaysToTryAgainLater()
    {
        dataRefreshHistoryRepository.TryTakeManualDecision(default, default, default, default).ReturnsForAnyArgs(false);
        dataRefreshHistoryRepository.FindRecord(pausedRecord.Id).Returns(
            pausedRecord,
            DataRefreshRecordBuilder.New.With(r => r.Id, pausedRecord.Id).Build());

        var resolution = await resolver.Resolve(Request(DataRefreshManualDecision.Complete));

        resolution.Message.Should().Contain("it is not paused").And.Contain("try again later");
    }

    #endregion

    #region Complete and RetryFailedBatches

    [TestCase(DataRefreshManualDecision.Complete)]
    [TestCase(DataRefreshManualDecision.RetryFailedBatches)]
    public async Task Resolve_CompleteOrRetry_TakesTheDecisionThenPublishesTheRequestOfTheRecord(DataRefreshManualDecision decision)
    {
        var resolution = await resolver.Resolve(Request(decision));

        resolution.Should().Be(new PausedDataRefreshResolution(
            PausedDataRefreshResolutionOutcome.Accepted, resolution.Message, pausedRecord.Id, decision));
        Received.InOrder(() =>
        {
            dataRefreshHistoryRepository.TryTakeManualDecision(pausedRecord.Id, decision, now.UtcDateTime, Arg.Any<string>());
            serviceBusClient.PublishToRequestTopic(Arg.Is<ValidatedDataRefreshRequest>(r => r.DataRefreshRecordId == pausedRecord.Id));
        });
    }

    [TestCase(DataRefreshManualDecision.Complete)]
    [TestCase(DataRefreshManualDecision.RetryFailedBatches)]
    public async Task Resolve_CompleteOrRetry_SendsTheEvent_AndNeitherNotifiesNorCancels(DataRefreshManualDecision decision)
    {
        await resolver.Resolve(Request(decision));

        manualDecisionEvents.Received(1).DecisionTaken(pausedRecord.Id, decision, Arg.Any<string>(), pausedRecord.AwaitingManualDecisionSinceUtc);
        await completionNotifier.DidNotReceiveWithAnyArgs().NotifyOfFailure(default);
        await precomputationRunCanceller.DidNotReceiveWithAnyArgs().CancelRun(default);
    }

    [TestCase(DataRefreshManualDecision.Complete)]
    [TestCase(DataRefreshManualDecision.RetryFailedBatches)]
    public async Task Resolve_WhenThePublishFails_ReturnsNotPublished_AndDoesNotUndoTheDecision(DataRefreshManualDecision decision)
    {
        serviceBusClient.PublishToRequestTopic(default).ThrowsAsyncForAnyArgs(new Exception(fixture.Create<string>()));

        var resolution = await resolver.Resolve(Request(decision));

        resolution.Outcome.Should().Be(PausedDataRefreshResolutionOutcome.NotPublished);
        resolution.Message.Should().Contain("Do not send the decision again");
        await dataRefreshHistoryRepository.Received(1).TryTakeManualDecision(pausedRecord.Id, decision, Arg.Any<DateTime>(), Arg.Any<string>());
        await dataRefreshHistoryRepository.DidNotReceiveWithAnyArgs().ClearManualDecisionState(default);
    }

    #endregion

    #region Fail

    [Test]
    public async Task Resolve_Fail_ClosesTheRecordThenNotifiesThenCancelsTheRun()
    {
        var resolution = await resolver.Resolve(Request(DataRefreshManualDecision.Fail));

        resolution.Outcome.Should().Be(PausedDataRefreshResolutionOutcome.Accepted);
        Received.InOrder(() =>
        {
            dataRefreshHistoryRepository.TryTakeManualDecision(pausedRecord.Id, DataRefreshManualDecision.Fail, now.UtcDateTime, Arg.Any<string>());
            completionNotifier.NotifyOfFailure(pausedRecord.Id);
            precomputationRunCanceller.CancelRun(pausedRecord);
        });
        await serviceBusClient.DidNotReceiveWithAnyArgs().PublishToRequestTopic(default);
    }

    [Test]
    public async Task Resolve_Fail_WhenTheNotificationFails_IsStillAccepted_AndStillCancelsTheRun()
    {
        completionNotifier.NotifyOfFailure(default).ThrowsAsyncForAnyArgs(new Exception(fixture.Create<string>()));

        var resolution = await resolver.Resolve(Request(DataRefreshManualDecision.Fail));

        resolution.Outcome.Should().Be(PausedDataRefreshResolutionOutcome.Accepted);
        await precomputationRunCanceller.Received(1).CancelRun(pausedRecord);
    }

    #endregion

    #region Who decided

    [Test]
    public async Task Resolve_PutsDecidedByIntoTheSupportCommentAndTheEvent()
    {
        var decidedBy = fixture.Create<string>();

        await resolver.Resolve(Request(DataRefreshManualDecision.RetryFailedBatches, decidedBy: decidedBy));

        await dataRefreshHistoryRepository.Received(1).TryTakeManualDecision(
            pausedRecord.Id,
            DataRefreshManualDecision.RetryFailedBatches,
            Arg.Any<DateTime>(),
            Arg.Is<string>(comment => comment.Contains($"Manual decision: {DataRefreshManualDecision.RetryFailedBatches}")
                                      && comment.Contains($"(by {decidedBy})")));
        manualDecisionEvents.Received(1).DecisionTaken(pausedRecord.Id, DataRefreshManualDecision.RetryFailedBatches, decidedBy, Arg.Any<DateTime?>());
    }

    [Test]
    public async Task Resolve_PutsDecidedByOnOneLine_AndCutsItToTheMaximumLength()
    {
        var decidedBy = string.Join(Environment.NewLine, fixture.CreateMany<string>(PausedDataRefreshResolver.MaxDecidedByLength));

        await resolver.Resolve(Request(DataRefreshManualDecision.Complete, decidedBy: decidedBy));

        manualDecisionEvents.Received(1).DecisionTaken(
            pausedRecord.Id,
            DataRefreshManualDecision.Complete,
            Arg.Is<string>(recorded => recorded.Length == PausedDataRefreshResolver.MaxDecidedByLength && !recorded.Contains('\n')),
            Arg.Any<DateTime?>());
    }

    [Test]
    public async Task Resolve_WithNoDecidedBy_PutsNoNameIntoTheSupportComment()
    {
        await resolver.Resolve(Request(DataRefreshManualDecision.Complete));

        await dataRefreshHistoryRepository.Received(1).TryTakeManualDecision(
            pausedRecord.Id,
            DataRefreshManualDecision.Complete,
            Arg.Any<DateTime>(),
            Arg.Is<string>(comment => !comment.Contains("(by")));
    }

    #endregion

    private ResolvePausedDataRefreshRequest Request(DataRefreshManualDecision decision, int? recordId = null, string decidedBy = null) =>
        new()
        {
            DataRefreshRecordId = recordId ?? pausedRecord.Id,
            Decision = decision,
            DecidedBy = decidedBy
        };
}
