using System.Diagnostics;
using Atlas.MatchPrediction.Worker.Settings;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Options;

namespace Atlas.MatchPrediction.Worker.Services;

/// <summary>
/// How many batch messages this replica is processing right now. Read by <see cref="WorkerRuntimeTelemetry"/>.
/// </summary>
public static class InFlightBatches
{
    private static int count;

    public static int Count => Volatile.Read(ref count);

    public static void Started() => Interlocked.Increment(ref count);

    public static void Finished() => Interlocked.Decrement(ref count);
}

/// <summary>
/// Sends one <see cref="EventName"/> event per replica every <see cref="MatchPredictionWorkerSettings.RuntimeTelemetryIntervalSeconds"/>,
/// with the process's CPU, garbage collection, thread pool and lock contention over the interval (ATL-474).
/// </summary>
/// <remarks>
/// <para>
/// Live imputation on Azure took about 1 s per donor while the replicas' CPU stayed mostly idle, and neither the Table
/// Storage reads (ATL-476) nor the frequency set reloads explained it. Those numbers tell apart the usual causes of
/// "slow but idle": long garbage collection pauses (<c>GcPausePercent</c>), a starved thread pool (<c>ThreadPoolQueueLength</c>
/// staying above zero, threads being added slowly), and lock contention (<c>LockContentionsPerSecond</c>).
/// </para>
/// <para>
/// Sent with its sampling percentage set to 100, like the genotype set timing event, so adaptive sampling never drops
/// it. Query: <c>customEvents | where name == "Match prediction worker runtime"</c>, by <c>cloud_RoleInstance</c>.
/// </para>
/// </remarks>
public class WorkerRuntimeTelemetry(
    TelemetryClient telemetryClient,
    IOptions<MatchPredictionWorkerSettings> settings,
    ILogger<WorkerRuntimeTelemetry> logger) : BackgroundService
{
    internal const string EventName = "Match prediction worker runtime";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = settings.Value.RuntimeTelemetryIntervalSeconds;
        if (intervalSeconds <= 0)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));
        var previous = RuntimeSample.Take();

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var current = RuntimeSample.Take();
                Send(previous, current);
                previous = current;
            }
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
    }

    private void Send(RuntimeSample previous, RuntimeSample current)
    {
        try
        {
            var telemetry = new EventTelemetry(EventName);
            foreach (var (name, value) in Metrics(previous, current))
            {
                telemetry.Metrics[name] = value;
            }

            telemetry.Properties["GcMode"] = System.Runtime.GCSettings.IsServerGC ? "Server" : "Workstation";
            ((ISupportSampling)telemetry).SamplingPercentage = 100;
            telemetryClient.TrackEvent(telemetry);
        }
        catch (Exception exception)
        {
            // Telemetry must never stop the worker.
            logger.LogWarning(exception, "Could not send the worker runtime event.");
        }
    }

    internal static Dictionary<string, double> Metrics(RuntimeSample previous, RuntimeSample current)
    {
        var seconds = Math.Max(0.001, (current.Timestamp - previous.Timestamp).TotalSeconds);
        var wallMs = seconds * 1000;

        return new Dictionary<string, double>
        {
            { "IntervalSeconds", seconds },
            { "InFlightBatches", current.InFlightBatches },
            // Of all the replica's cores: 100 means every core busy for the whole interval.
            { "CpuPercent", (current.ProcessorTime - previous.ProcessorTime).TotalMilliseconds / (wallMs * current.ProcessorCount) * 100 },
            { "ProcessorCount", current.ProcessorCount },
            { "GcPausePercent", (current.GcPauseTime - previous.GcPauseTime).TotalMilliseconds / wallMs * 100 },
            { "Gen0Collections", current.Gen0Collections - previous.Gen0Collections },
            { "Gen1Collections", current.Gen1Collections - previous.Gen1Collections },
            { "Gen2Collections", current.Gen2Collections - previous.Gen2Collections },
            { "GcHeapMB", current.GcHeapBytes / 1048576.0 },
            { "WorkingSetMB", current.WorkingSetBytes / 1048576.0 },
            { "GcMemoryLimitMB", current.GcMemoryLimitBytes / 1048576.0 },
            { "AllocatedMBPerSecond", (current.AllocatedBytes - previous.AllocatedBytes) / 1048576.0 / seconds },
            { "ThreadPoolThreads", current.ThreadPoolThreads },
            { "ThreadPoolQueueLength", current.ThreadPoolQueueLength },
            { "ThreadPoolCompletedPerSecond", (current.ThreadPoolCompleted - previous.ThreadPoolCompleted) / seconds },
            { "LockContentionsPerSecond", (current.LockContentions - previous.LockContentions) / seconds },
        };
    }
}

/// <summary>One reading of the process's runtime counters.</summary>
internal sealed record RuntimeSample(
    DateTimeOffset Timestamp,
    int InFlightBatches,
    TimeSpan ProcessorTime,
    int ProcessorCount,
    TimeSpan GcPauseTime,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections,
    long GcHeapBytes,
    long WorkingSetBytes,
    long GcMemoryLimitBytes,
    long AllocatedBytes,
    int ThreadPoolThreads,
    long ThreadPoolQueueLength,
    long ThreadPoolCompleted,
    long LockContentions)
{
    public static RuntimeSample Take()
    {
        using var process = Process.GetCurrentProcess();
        var gcInfo = GC.GetGCMemoryInfo();

        return new RuntimeSample(
            DateTimeOffset.UtcNow,
            Services.InFlightBatches.Count,
            process.TotalProcessorTime,
            Environment.ProcessorCount,
            GC.GetTotalPauseDuration(),
            GC.CollectionCount(0),
            GC.CollectionCount(1),
            GC.CollectionCount(2),
            gcInfo.HeapSizeBytes,
            process.WorkingSet64,
            gcInfo.TotalAvailableMemoryBytes,
            GC.GetTotalAllocatedBytes(),
            ThreadPool.ThreadCount,
            ThreadPool.PendingWorkItemCount,
            ThreadPool.CompletedWorkItemCount,
            Monitor.LockContentionCount);
    }
}
