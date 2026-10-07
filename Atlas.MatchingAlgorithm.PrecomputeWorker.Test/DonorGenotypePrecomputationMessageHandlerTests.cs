using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using AutoFixture;
using AwesomeAssertions;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.PrecomputeWorker.Test;

[TestFixture]
internal class DonorGenotypePrecomputationMessageHandlerTests
{
    private Fixture fixture = null!;
    private IDonorGenotypePrecomputationBatchProcessor processor = null!;
    private ServiceProvider provider = null!;
    private DonorGenotypePrecomputationMessageHandler handler = null!;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        processor = Substitute.For<IDonorGenotypePrecomputationBatchProcessor>();

        var services = new ServiceCollection();
        services.AddScoped(_ => processor);
        provider = services.BuildServiceProvider();

        handler = new DonorGenotypePrecomputationMessageHandler(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<DonorGenotypePrecomputationMessageHandler>.Instance);
    }

    [TearDown]
    public async Task TearDown()
    {
        await provider.DisposeAsync();
    }

    [Test]
    public async Task Handle_ProcessesTheBatchThatTheMessageNames()
    {
        var request = fixture.Create<DonorGenotypePrecomputationBatchRequest>();

        await handler.Handle(Message(JsonConvert.SerializeObject(request), deliveryCount: 1));

        await processor.Received(1).ProcessBatch(
            Arg.Is<DonorGenotypePrecomputationBatchRequest>(processed =>
                processed.DataRefreshRecordId == request.DataRefreshRecordId
                && processed.RunId == request.RunId
                && processed.BatchId == request.BatchId),
            Arg.Any<bool>());
    }

    [TestCase(1, false)]
    [TestCase(2, true)]
    [TestCase(10, true)]
    public async Task Handle_TellsTheProcessorWhetherTheMessageIsARedelivery(int deliveryCount, bool isRedelivery)
    {
        // A redelivery can take the batch from a live lease: the worker before stopped without a result.
        await handler.Handle(Message(JsonConvert.SerializeObject(fixture.Create<DonorGenotypePrecomputationBatchRequest>()), deliveryCount));

        await processor.Received(1).ProcessBatch(Arg.Any<DonorGenotypePrecomputationBatchRequest>(), isRedelivery);
    }

    [TestCase(DonorGenotypePrecomputationBatchResult.ResultsReceived)]
    [TestCase(DonorGenotypePrecomputationBatchResult.Failed)]
    [TestCase(DonorGenotypePrecomputationBatchResult.Skipped)]
    [TestCase(DonorGenotypePrecomputationBatchResult.LeaseLost)]
    public async Task Handle_WhenTheBatchIsDoneWith_CompletesTheMessage(DonorGenotypePrecomputationBatchResult result)
    {
        // Also a failed batch: the failure is on its batch row, and the requeue sweep sends a new message for it.
        processor.ProcessBatch(default!, default).ReturnsForAnyArgs(result);

        var decision = await handler.Handle(Message(JsonConvert.SerializeObject(fixture.Create<DonorGenotypePrecomputationBatchRequest>())));

        decision.Should().Be(MessageDecision.Complete);
    }

    [Test]
    public async Task Handle_WhenTheBatchCannotBeFinished_AbandonsTheMessage()
    {
        // Service Bus then delivers it again, and after its last delivery dead-letters it.
        processor.ProcessBatch(default!, default).ThrowsAsyncForAnyArgs(new InvalidOperationException(fixture.Create<string>()));

        var decision = await handler.Handle(Message(JsonConvert.SerializeObject(fixture.Create<DonorGenotypePrecomputationBatchRequest>())));

        decision.Should().Be(MessageDecision.Abandon);
    }

    [Test]
    public async Task Handle_ForABodyThatIsNotARequest_DeadLettersTheMessage()
    {
        // Every delivery would read the same body, so a retry cannot help.
        var decision = await handler.Handle(Message(fixture.Create<string>()));

        decision.Settlement.Should().Be(MessageSettlement.DeadLetter);
        decision.DeadLetterReason.Should().Be(DonorGenotypePrecomputationMessageHandler.UnreadableBodyReason);
        await processor.DidNotReceiveWithAnyArgs().ProcessBatch(default!, default);
    }

    [Test]
    public async Task Handle_ForABodyThatNamesNoBatch_DeadLettersTheMessage()
    {
        var decision = await handler.Handle(Message("{}"));

        decision.Settlement.Should().Be(MessageSettlement.DeadLetter);
    }

    private static ServiceBusReceivedMessage Message(string body, int deliveryCount = 1) =>
        ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString(body),
            messageId: Guid.NewGuid().ToString(),
            deliveryCount: deliveryCount);
}
