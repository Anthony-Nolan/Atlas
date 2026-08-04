using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh
{
    /// <summary>
    /// The sampler runs for the whole of a ~15-hour job, so the properties that matter are the lifecycle ones: it must
    /// never fail the job it is measuring, and it must never outlive it. The sampling interval is 30s, so these tests
    /// deliberately assert on start/stop behaviour rather than waiting for a sample.
    /// </summary>
    [TestFixture]
    public class DataRefreshRuntimeSamplerTests
    {
        private IMatchingAlgorithmImportLogger logger;
        private IDataRefreshRuntimeSampler sampler;

        [SetUp]
        public void SetUp()
        {
            logger = Substitute.For<IMatchingAlgorithmImportLogger>();
            sampler = new DataRefreshRuntimeSampler(logger);
        }

        [Test]
        public void StartSampling_DoesNotThrow()
        {
            var act = () => sampler.StartSampling();

            act.Should().NotThrow();
        }

        [Test]
        public async Task DisposeAsync_StopsTheSamplingLoopWithoutThrowing()
        {
            var session = sampler.StartSampling();

            // Cancelling the loop's PeriodicTimer surfaces as an OperationCanceledException on the retained task;
            // disposal must absorb it rather than let it escape into the refresh's own failure handling.
            var act = async () => await session.DisposeAsync();

            await act.Should().NotThrowAsync();
        }

        [Test]
        public async Task DisposeAsync_AfterStoppingTheLoop_EmitsNoFurtherSamples()
        {
            var session = sampler.StartSampling();
            await session.DisposeAsync();
            logger.ClearReceivedCalls();

            // DisposeAsync awaits the loop task, so by the time it returns nothing can still be in flight.
            await Task.Delay(TimeSpan.FromMilliseconds(50));

            logger.DidNotReceiveWithAnyArgs().SendMetric(default, default, default);
        }

        [Test]
        public async Task StartSampling_TakesNoSampleBeforeTheFirstTick()
        {
            var session = sampler.StartSampling();

            await Task.Delay(TimeSpan.FromMilliseconds(50));

            // A run that dies in its first few seconds should leave a clean, empty runtime series rather than one
            // unrepresentative sample taken before the process reached steady state.
            logger.DidNotReceive().SendMetric(
                DataRefreshMetrics.RuntimeMetric,
                Arg.Any<double>(),
                Arg.Any<Dictionary<string, string>>());

            await session.DisposeAsync();
        }
    }
}
