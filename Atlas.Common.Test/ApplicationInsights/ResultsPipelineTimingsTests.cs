using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using AwesomeAssertions;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.Common.Test.ApplicationInsights
{
    [TestFixture]
    public class ResultsPipelineTimingsTests
    {
        private IAtlasLogger logger;
        private Dictionary<string, string> sentProps;
        private Dictionary<string, double> sentMetrics;

        [SetUp]
        public void SetUp()
        {
            sentProps = null;
            sentMetrics = null;
            logger = Substitute.For<IAtlasLogger>();
            logger.When(l => l.SendEvent(ResultsPipelineTimings.EventName, Arg.Any<LogLevel>(), Arg.Any<Dictionary<string, string>>(),
                    Arg.Any<Dictionary<string, double>>()))
                .Do(call =>
                {
                    sentProps = call.ArgAt<Dictionary<string, string>>(2);
                    sentMetrics = call.ArgAt<Dictionary<string, double>>(3);
                });
        }

        [Test]
        public void StaticMethods_WhenNoCollectionIsRunning_DoNothing()
        {
            using (ResultsPipelineTimings.Time("Phase"))
            {
                ResultsPipelineTimings.Add("Count", 1);
                ResultsPipelineTimings.AddBlobRead(1024);
                ResultsPipelineTimings.Set("Name", "Value");
            }

            logger.DidNotReceiveWithAnyArgs().SendEvent(default);
        }

        [Test]
        public async Task Dispose_SendsOneEventWithStepIdsPhasesAndCounts()
        {
            using (ResultsPipelineTimings.Begin(logger, "Step", "search-id", "repeat-id", deliveryCount: 2, enqueuedTimeUtc: DateTime.UtcNow))
            {
                await AddFromAnotherAsyncMethod();
                await AddFromAnotherAsyncMethod();
                ResultsPipelineTimings.AddBlobRead(2 * 1024 * 1024);
                ResultsPipelineTimings.AddBlobWritten(1024 * 1024);
            }

            logger.ReceivedWithAnyArgs(1).SendEvent(default);
            sentProps["Step"].Should().Be("Step");
            sentProps["SearchRequestId"].Should().Be("search-id");
            sentProps["RepeatSearchId"].Should().Be("repeat-id");
            sentProps["Outcome"].Should().Be("Succeeded");
            sentMetrics["DeliveryCount"].Should().Be(2);
            sentMetrics["PhaseCalls"].Should().Be(2);
            sentMetrics.Should().ContainKey("PhaseMs");
            sentMetrics["Count"].Should().Be(6);
            sentMetrics["BlobReadFiles"].Should().Be(1);
            sentMetrics["BlobReadMb"].Should().Be(2);
            sentMetrics["BlobWrittenMb"].Should().Be(1);
            sentMetrics.Should().ContainKeys("TotalMs", "AllocatedMb", "PeakWorkingSetMb", "MessageAgeAtStartMs", "ConcurrentStepsMax");
        }

        [Test]
        public void Fail_SendsFailedOutcomeAndExceptionTypeOnly()
        {
            using (var timings = ResultsPipelineTimings.Begin(logger, "Step", "search-id"))
            {
                timings.Fail(new InvalidOperationException("message with details"));
            }

            sentProps["Outcome"].Should().Be("Failed");
            sentProps["ExceptionType"].Should().Be(nameof(InvalidOperationException));
            sentProps.Values.Should().NotContain(v => v != null && v.Contains("message with details"));
        }

        [Test]
        public void Dispose_StopsTheCollection()
        {
            using (ResultsPipelineTimings.Begin(logger, "Step", "search-id"))
            {
            }

            ResultsPipelineTimings.Add("Count", 1);

            logger.ReceivedWithAnyArgs(1).SendEvent(default);
            sentMetrics.Should().NotContainKey("Count");
        }

        [Test]
        public void TelemetryInitializer_SetsSamplingPercentageTo100_OnlyForTimingsEvents()
        {
            var initializer = new ResultsPipelineTimingsTelemetryInitializer();
            var timingsEvent = new EventTelemetry(ResultsPipelineTimings.EventName);
            var otherEvent = new EventTelemetry("Other");

            initializer.Initialize(timingsEvent);
            initializer.Initialize(otherEvent);

            ((ISupportSampling)timingsEvent).SamplingPercentage.Should().Be(100);
            ((ISupportSampling)otherEvent).SamplingPercentage.Should().BeNull();
        }

        private static async Task AddFromAnotherAsyncMethod()
        {
            using (ResultsPipelineTimings.Time("Phase"))
            {
                await Task.Yield();
                ResultsPipelineTimings.Add("Count", 3);
            }
        }
    }
}
