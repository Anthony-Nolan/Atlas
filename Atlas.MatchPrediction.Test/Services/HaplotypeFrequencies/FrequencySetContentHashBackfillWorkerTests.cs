using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.HaplotypeFrequencies;

[TestFixture]
internal class FrequencySetContentHashBackfillWorkerTests
{
    private Fixture fixture;
    private IFrequencySetContentHashBackfill backfill;
    private ServiceProvider serviceProvider;
    private FakeLogger<FrequencySetContentHashBackfillWorker> logger;
    private FrequencySetContentHashBackfillWorker sut;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        backfill = Substitute.For<IFrequencySetContentHashBackfill>();
        serviceProvider = new ServiceCollection().AddScoped(_ => backfill).BuildServiceProvider();
        logger = new FakeLogger<FrequencySetContentHashBackfillWorker>();

        sut = new FrequencySetContentHashBackfillWorker(serviceProvider.GetRequiredService<IServiceScopeFactory>(), logger);
    }

    [TearDown]
    public async Task TearDown()
    {
        await sut.StopAsync(CancellationToken.None);
        sut.Dispose();
        await serviceProvider.DisposeAsync();
    }

    [Test]
    public async Task Start_RunsTheBackfillInTheBackground()
    {
        var run = new TaskCompletionSource<int>();
        backfill.Run(default).ReturnsForAnyArgs(run.Task);
        await sut.StartAsync(CancellationToken.None);

        sut.Start();
        await WaitUntil(() => backfill.ReceivedCalls().Any());

        sut.IsRunning.Should().BeTrue();
        run.SetResult(fixture.Create<int>());
        await WaitUntil(() => !sut.IsRunning);
    }

    [Test]
    public async Task Start_DuringARun_StartsNoOtherRun()
    {
        var run = new TaskCompletionSource<int>();
        backfill.Run(default).ReturnsForAnyArgs(run.Task, Task.FromResult(fixture.Create<int>()));
        await sut.StartAsync(CancellationToken.None);

        sut.Start();
        await WaitUntil(() => backfill.ReceivedCalls().Any());
        sut.Start();
        run.SetResult(fixture.Create<int>());
        await WaitUntil(() => !sut.IsRunning);

        // A second run would start at once: the worker has nothing else to wait for.
        await Task.Delay(TimeSpan.FromMilliseconds(100));
        await backfill.ReceivedWithAnyArgs(1).Run(default);
    }

    [Test]
    public async Task Start_AfterARunEnded_RunsTheBackfillAgain()
    {
        backfill.Run(default).ReturnsForAnyArgs(Task.FromResult(fixture.Create<int>()));
        await sut.StartAsync(CancellationToken.None);

        sut.Start();
        await WaitUntil(() => !sut.IsRunning && backfill.ReceivedCalls().Count() == 1);
        sut.Start();
        await WaitUntil(() => !sut.IsRunning && backfill.ReceivedCalls().Count() == 2);

        await backfill.ReceivedWithAnyArgs(2).Run(default);
    }

    /// <summary>A failed run must not stop the worker: the next start runs the backfill again.</summary>
    [Test]
    public async Task RunOnce_WhenTheBackfillFails_LogsAnError_AndThrowsNothing()
    {
        backfill.Run(default).ReturnsForAnyArgs(Task.FromException<int>(new InvalidOperationException(fixture.Create<string>())));
        sut.Start();

        var runOnce = () => sut.RunOnce(CancellationToken.None);

        await runOnce.Should().NotThrowAsync();
        sut.IsRunning.Should().BeFalse();
        logger.Collector.GetSnapshot().Should().ContainSingle().Which.Level.Should().Be(LogLevel.Error);
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
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
