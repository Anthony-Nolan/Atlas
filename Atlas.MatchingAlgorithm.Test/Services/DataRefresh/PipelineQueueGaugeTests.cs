using Atlas.MatchingAlgorithm.Services.DataRefresh;
using AutoFixture;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh
{
    /// <summary>
    /// The gauge exists so that a pipeline which overlapped can be told apart from one whose consumer sat starved -
    /// two things a stage's wall clock reports identically. The properties worth pinning are therefore that a starved
    /// take is counted, and that the count the sampler reads is a delta rather than a running total.
    /// </summary>
    [TestFixture]
    public class PipelineQueueGaugeTests
    {
        private Fixture fixture;
        private PipelineQueueGauge gauge;

        [SetUp]
        public void SetUp()
        {
            fixture = new Fixture();
            gauge = new PipelineQueueGauge();
        }

        [Test]
        public void Depth_BeforeAnyTake_IsZero()
        {
            gauge.Depth.Should().Be(0);
        }

        [Test]
        public void Depth_ReportsTheMostRecentTake()
        {
            var firstDepth = fixture.Create<int>();
            var latestDepth = fixture.Create<int>();

            gauge.RecordDepthOnTake(firstDepth);
            gauge.RecordDepthOnTake(latestDepth);

            gauge.Depth.Should().Be(latestDepth);
        }

        [Test]
        public void RecordDepthOnTake_WhenNothingWasQueuedBehindTheItem_CountsAStarvation()
        {
            gauge.RecordDepthOnTake(0);

            gauge.ReadStarvationsSinceLastRead().Should().Be(1);
        }

        [Test]
        public void RecordDepthOnTake_WhenWorkWasQueuedBehindTheItem_CountsNoStarvation()
        {
            gauge.RecordDepthOnTake(fixture.Create<int>());

            gauge.ReadStarvationsSinceLastRead().Should().Be(0);
        }

        [Test]
        public void ReadStarvationsSinceLastRead_ReportsADeltaAndNotARunningTotal()
        {
            gauge.RecordDepthOnTake(0);
            gauge.RecordDepthOnTake(0);
            gauge.ReadStarvationsSinceLastRead().Should().Be(2);

            gauge.RecordDepthOnTake(0);

            // The sampler's other counters are deltas per interval, and a running total would render as a ramp on the
            // timechart rather than as the rate the question is actually about.
            gauge.ReadStarvationsSinceLastRead().Should().Be(1);
        }

        [Test]
        public void ReadStarvationsSinceLastRead_WhenNothingStarvedSinceTheLastRead_IsZero()
        {
            gauge.RecordDepthOnTake(0);
            gauge.ReadStarvationsSinceLastRead();

            gauge.ReadStarvationsSinceLastRead().Should().Be(0);
        }
    }
}
