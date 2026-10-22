using Atlas.MatchPrediction.Worker.Services;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Worker.Test.Services;

[TestFixture]
public class WorkerRuntimeTelemetryTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 22, 9, 0, 0, TimeSpan.Zero);

    [Test]
    public void Metrics_TurnTwoReadingsIntoRatesAndDifferencesOverTheInterval()
    {
        var previous = Sample(Start, processorTime: TimeSpan.FromSeconds(10), gcPause: TimeSpan.FromSeconds(1),
            gen0: 100, gen2: 3, allocated: 1_000L * 1048576, completed: 1000, contentions: 50);
        var current = Sample(Start.AddSeconds(10), processorTime: TimeSpan.FromSeconds(15), gcPause: TimeSpan.FromSeconds(3),
            gen0: 130, gen2: 4, allocated: 1_500L * 1048576, completed: 6000, contentions: 150);

        var metrics = WorkerRuntimeTelemetry.Metrics(previous, current);

        metrics["IntervalSeconds"].Should().Be(10);
        // 5 s of CPU over 10 s on 2 cores.
        metrics["CpuPercent"].Should().BeApproximately(25, 0.001);
        // 2 s of GC pause over 10 s.
        metrics["GcPausePercent"].Should().BeApproximately(20, 0.001);
        metrics["Gen0Collections"].Should().Be(30);
        metrics["Gen1Collections"].Should().Be(0);
        metrics["Gen2Collections"].Should().Be(1);
        metrics["AllocatedMBPerSecond"].Should().BeApproximately(50, 0.001);
        metrics["ThreadPoolCompletedPerSecond"].Should().Be(500);
        metrics["LockContentionsPerSecond"].Should().Be(10);
        metrics["ThreadPoolQueueLength"].Should().Be(7);
        metrics["InFlightBatches"].Should().Be(3);
        metrics["GcHeapMB"].Should().Be(2048);
    }

    private static RuntimeSample Sample(
        DateTimeOffset timestamp,
        TimeSpan processorTime,
        TimeSpan gcPause,
        int gen0,
        int gen2,
        long allocated,
        long completed,
        long contentions) =>
        new(
            timestamp,
            InFlightBatches: 3,
            ProcessorTime: processorTime,
            ProcessorCount: 2,
            GcPauseTime: gcPause,
            Gen0Collections: gen0,
            Gen1Collections: 5,
            Gen2Collections: gen2,
            GcHeapBytes: 2048L * 1048576,
            WorkingSetBytes: 3000L * 1048576,
            GcMemoryLimitBytes: 4096L * 1048576,
            AllocatedBytes: allocated,
            ThreadPoolThreads: 12,
            ThreadPoolQueueLength: 7,
            ThreadPoolCompleted: completed,
            LockContentions: contentions);
}
