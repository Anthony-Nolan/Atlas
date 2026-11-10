using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using AwesomeAssertions;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.PrecomputeWorker.Test;

/// <summary>
/// The start and the stop of the worker: it warms the HLA Metadata Dictionary caches before it takes its first message,
/// and a stop during the warm-up does not start processing.
/// </summary>
[TestFixture]
internal class DonorGenotypePrecomputationWorkerTests
{
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(5);

    private ServiceBusProcessor processor = null!;
    private TaskCompletionSource warmUp = null!;
    private IDonorGenotypePrecomputationCacheWarmer cacheWarmer = null!;
    private ServiceProvider provider = null!;
    private DonorGenotypePrecomputationWorker worker = null!;

    [SetUp]
    public void SetUp()
    {
        processor = Substitute.For<ServiceBusProcessor>();

        // The warm-up ends when a test ends it.
        warmUp = new TaskCompletionSource();
        cacheWarmer = Substitute.For<IDonorGenotypePrecomputationCacheWarmer>();
        cacheWarmer.WarmUp().Returns(warmUp.Task);

        // The handler gets no message in these tests.
        provider = new ServiceCollection().BuildServiceProvider();
        var handler = new DonorGenotypePrecomputationMessageHandler(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Substitute.For<IDonorGenotypePrecomputationMetrics>(),
            NullLogger<DonorGenotypePrecomputationMessageHandler>.Instance);

        worker = new DonorGenotypePrecomputationWorker(processor, handler, cacheWarmer, NullLogger<DonorGenotypePrecomputationWorker>.Instance);
    }

    [TearDown]
    public async Task TearDown()
    {
        // Dispose stops a worker that a test did not stop.
        worker.Dispose();
        await processor.DisposeAsync();
        await provider.DisposeAsync();
    }

    [Test]
    public async Task StartAsync_StartsProcessingOnlyAfterTheWarmUpEnds()
    {
        await worker.StartAsync(CancellationToken.None);
        await WaitUntil(() => cacheWarmer.ReceivedCalls().Any());

        await processor.DidNotReceiveWithAnyArgs().StartProcessingAsync(default);

        warmUp.SetResult();
        await worker.ExecuteTask!.WaitAsync(WaitLimit);

        await processor.ReceivedWithAnyArgs(1).StartProcessingAsync(default);
    }

    [Test]
    public async Task StopAsync_DuringTheWarmUp_EndsTheStart_AndDoesNotStartProcessing()
    {
        await worker.StartAsync(CancellationToken.None);
        await WaitUntil(() => cacheWarmer.ReceivedCalls().Any());

        await worker.StopAsync(CancellationToken.None).WaitAsync(WaitLimit);

        worker.ExecuteTask!.IsCompleted.Should().BeTrue();
        await processor.DidNotReceiveWithAnyArgs().StartProcessingAsync(default);
    }

    [Test]
    public async Task StopAsync_WhenTheWarmUpEndsDuringTheStop_DoesNotStartProcessing()
    {
        // The warm-up ends while the processor stops.
        processor.StopProcessingAsync(default).ReturnsForAnyArgs(_ =>
        {
            warmUp.TrySetResult();
            return Task.CompletedTask;
        });
        await worker.StartAsync(CancellationToken.None);
        await WaitUntil(() => cacheWarmer.ReceivedCalls().Any());

        await worker.StopAsync(CancellationToken.None).WaitAsync(WaitLimit);

        await processor.DidNotReceiveWithAnyArgs().StartProcessingAsync(default);
    }

    [Test]
    public async Task StopAsync_AfterProcessingStarted_StopsProcessing()
    {
        await worker.StartAsync(CancellationToken.None);
        warmUp.SetResult();
        await worker.ExecuteTask!.WaitAsync(WaitLimit);

        await worker.StopAsync(CancellationToken.None).WaitAsync(WaitLimit);

        await processor.ReceivedWithAnyArgs(1).StopProcessingAsync(default);
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.Add(WaitLimit);
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(10);
        }

        throw new TimeoutException("Condition was not met within the allotted time.");
    }
}
