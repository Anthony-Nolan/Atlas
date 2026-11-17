using Atlas.Common.AzureStorage.Blob;
using Atlas.Common.ServiceBus;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Worker.Services;
using Atlas.SearchTracking.Common.Dispatchers;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Worker.Test.Services;

[TestFixture]
internal class ParallelMatchPredictionBatchRunnerTests
{
    private const int MaxParallelism = 4;
    private const bool RetriesLeft = false;
    private const bool LastDelivery = true;

    private IBlobDownloader blobDownloader;
    private IParallelMatchPredictionAlgorithm algorithm;
    private ISessionMessagePublisher<ParallelMatchPredictionBatchResult> resultPublisher;
    private IMatchPredictionSearchTrackingDispatcher trackingDispatcher;
    private ParallelMatchPredictionBatchRunner runner;

    private Fixture fixture;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        blobDownloader = Substitute.For<IBlobDownloader>();
        algorithm = Substitute.For<IParallelMatchPredictionAlgorithm>();
        resultPublisher = Substitute.For<ISessionMessagePublisher<ParallelMatchPredictionBatchResult>>();
        trackingDispatcher = Substitute.For<IMatchPredictionSearchTrackingDispatcher>();

        blobDownloader.Download<MultipleDonorMatchProbabilityInput>(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new MultipleDonorMatchProbabilityInput());
        algorithm.RunBatch(Arg.Any<MultipleDonorMatchProbabilityInput>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new ParallelMatchPredictionBatchOutput(fixture.Create<string>(), fixture.Create<int>(), new Dictionary<int, int>()));

        runner = new ParallelMatchPredictionBatchRunner(
            blobDownloader,
            algorithm,
            resultPublisher,
            trackingDispatcher,
            Options.Create(new AzureStorageSettings { MatchPredictionRequestsBlobContainer = fixture.Create<string>() }),
            Options.Create(new MatchPredictionRequestsSettings { MaxParallelism = MaxParallelism }),
            Substitute.For<ILogger<ParallelMatchPredictionBatchRunner>>());
    }

    [Test]
    public async Task RunBatch_WhenSuccessful_PublishesSuccessResultWithSingleLocationAndEchoedIds()
    {
        var request = BuildRequest(isRepeatSearch: false);
        var resultLocation = fixture.Create<string>();
        algorithm.RunBatch(Arg.Any<MultipleDonorMatchProbabilityInput>(), MaxParallelism, request.BatchId)
            .Returns(new ParallelMatchPredictionBatchOutput(resultLocation, fixture.Create<int>(), new Dictionary<int, int>()));

        await runner.RunBatch(request, RetriesLeft);

        await resultPublisher.Received(1).PublishWithSession(
            Arg.Is<ParallelMatchPredictionBatchResult>(r =>
                r.IsSuccessful
                && r.MatchPredictionResultLocation == resultLocation
                && r.BatchId == request.BatchId
                && r.ParallelRunId == request.ParallelRunId
                && r.BatchSequenceNumber == request.BatchSequenceNumber),
            request.SearchRequestId);
    }

    [Test]
    public async Task RunBatch_WhenSuccessful_PublishesGenotypeCountsFromAlgorithmOutput()
    {
        var request = BuildRequest(isRepeatSearch: false);
        const int patientGenotypeCount = 7;
        var donorGenotypeCounts = new Dictionary<int, int> { { 1, 12 }, { 2, 34 } };
        algorithm.RunBatch(Arg.Any<MultipleDonorMatchProbabilityInput>(), MaxParallelism, request.BatchId)
            .Returns(new ParallelMatchPredictionBatchOutput(fixture.Create<string>(), patientGenotypeCount, donorGenotypeCounts));

        await runner.RunBatch(request, RetriesLeft);

        await resultPublisher.Received(1).PublishWithSession(
            Arg.Is<ParallelMatchPredictionBatchResult>(r =>
                r.IsSuccessful
                && r.PatientGenotypeCount == patientGenotypeCount
                && r.DonorGenotypeCounts.Count == 2
                && r.DonorGenotypeCounts[1] == 12
                && r.DonorGenotypeCounts[2] == 34),
            request.SearchRequestId);
    }

    [Test]
    public async Task RunBatch_WhenNotRepeatSearch_DispatchesTrackingWithSearchIdAndNoOriginal()
    {
        var request = BuildRequest(isRepeatSearch: false);

        await runner.RunBatch(request, RetriesLeft);

        await trackingDispatcher.Received(1).ProcessRunningBatchesStarted(Guid.Parse(request.SearchRequestId), null);
        await trackingDispatcher.Received(1).ProcessRunningBatchesEnded(Guid.Parse(request.SearchRequestId), null);
    }

    [Test]
    public async Task RunBatch_WhenRepeatSearch_DispatchesTrackingWithRepeatIdAsSearchAndOriginalAsSearchRequestId()
    {
        var request = BuildRequest(isRepeatSearch: true);

        await runner.RunBatch(request, RetriesLeft);

        var expectedSearchIdentifier = Guid.Parse(request.RepeatSearchRequestId);
        var expectedOriginalIdentifier = Guid.Parse(request.SearchRequestId);
        await trackingDispatcher.Received(1).ProcessRunningBatchesStarted(expectedSearchIdentifier, expectedOriginalIdentifier);
        await resultPublisher.Received(1).PublishWithSession(
            Arg.Is<ParallelMatchPredictionBatchResult>(r =>
                r.SearchIdentifier == new Guid(request.SearchRequestId)
                && r.RepeatSearchIdentifier == new Guid(request.RepeatSearchRequestId)),
            request.SearchRequestId);
    }

    /// <remarks>
    /// Before the last delivery the batch will be retried, so nothing is reported: a failure result would make the
    /// finaliser send a failure notification, and a successful retry would then send a success for the same search.
    /// The rethrow is what makes Service Bus retry (or dead-letter) the message.
    /// </remarks>
    [TestCase(RetriesLeft, 0)]
    [TestCase(LastDelivery, 1)]
    public async Task RunBatch_WhenProcessingThrows_PublishesFailureResultOnlyOnLastDelivery_AndRethrows(
        bool isLastDelivery,
        int expectedFailureResultCount)
    {
        var request = BuildRequest(isRepeatSearch: false);
        var thrown = new InvalidOperationException(fixture.Create<string>());
        blobDownloader.Download<MultipleDonorMatchProbabilityInput>(Arg.Any<string>(), Arg.Any<string>()).ThrowsAsync(thrown);

        var act = () => runner.RunBatch(request, isLastDelivery);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await resultPublisher.Received(expectedFailureResultCount)
            .PublishWithSession(Arg.Any<ParallelMatchPredictionBatchResult>(), Arg.Any<string>());
        await resultPublisher.Received(expectedFailureResultCount).PublishWithSession(
            Arg.Is<ParallelMatchPredictionBatchResult>(r =>
                !r.IsSuccessful
                && r.FailureMessage == thrown.Message
                && r.FailureException.Contains(thrown.GetType().FullName!)
                && r.FailureException.Contains(thrown.Message)
                && r.BatchId == request.BatchId
                && r.ParallelRunId == request.ParallelRunId
                && r.BatchSequenceNumber == request.BatchSequenceNumber),
            request.SearchRequestId);
    }

    /// <remarks>
    /// ATL-546: once the batch is computed, its result is in a blob, so an error while reporting it is not a batch
    /// failure, even on the last delivery. The error is rethrown, so Service Bus delivers the batch again.
    /// </remarks>
    [TestCase(true, TestName = "RunBatch_WhenSuccessResultPublishThrowsOnLastDelivery_PublishesNoFailureResult_AndRethrows")]
    [TestCase(false, TestName = "RunBatch_WhenEndedTrackingThrowsOnLastDelivery_PublishesNoFailureResult_AndRethrows")]
    public async Task RunBatch_WhenReportingComputedBatchThrows_PublishesNoFailureResult_AndRethrows(bool successPublishThrows)
    {
        var request = BuildRequest(isRepeatSearch: false);
        var thrown = new InvalidOperationException(fixture.Create<string>());
        if (successPublishThrows)
        {
            resultPublisher.PublishWithSession(Arg.Is<ParallelMatchPredictionBatchResult>(r => r.IsSuccessful), Arg.Any<string>())
                .ThrowsAsync(thrown);
        }
        else
        {
            trackingDispatcher.ProcessRunningBatchesEnded(Arg.Any<Guid>(), Arg.Any<Guid?>()).ThrowsAsync(thrown);
        }

        var act = () => runner.RunBatch(request, LastDelivery);

        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(thrown);
        await algorithm.Received(1).RunBatch(Arg.Any<MultipleDonorMatchProbabilityInput>(), MaxParallelism, request.BatchId);
        await resultPublisher.DidNotReceive()
            .PublishWithSession(Arg.Is<ParallelMatchPredictionBatchResult>(r => !r.IsSuccessful), Arg.Any<string>());
    }

    [Test]
    public async Task RunBatch_WhenFailureResultPublishItselfThrows_Propagates()
    {
        var request = BuildRequest(isRepeatSearch: false);
        blobDownloader.Download<MultipleDonorMatchProbabilityInput>(Arg.Any<string>(), Arg.Any<string>())
            .ThrowsAsync(new InvalidOperationException(fixture.Create<string>()));
        resultPublisher.PublishWithSession(Arg.Any<ParallelMatchPredictionBatchResult>(), Arg.Any<string>())
            .ThrowsAsync(new Exception(fixture.Create<string>()));

        var act = () => runner.RunBatch(request, LastDelivery);

        await act.Should().ThrowAsync<Exception>();
    }

    private ParallelMatchPredictionBatchRequest BuildRequest(bool isRepeatSearch) => new()
    {
        BlobLocation = fixture.Create<string>(),
        SearchRequestId = Guid.NewGuid().ToString(),
        IsRepeatSearch = isRepeatSearch,
        RepeatSearchRequestId = isRepeatSearch ? Guid.NewGuid().ToString() : null,
        ParallelRunId = fixture.Create<int>(),
        BatchId = fixture.Create<int>(),
        BatchSequenceNumber = fixture.Create<int>(),
    };
}
