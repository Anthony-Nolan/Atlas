using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh;

public interface IDataRefreshRuntimeSampler
{
    /// <summary>
    /// Starts periodically sampling process utilisation, until the returned handle is disposed.
    /// The caller MUST dispose it on every exit path, success or failure.
    /// </summary>
    IAsyncDisposable StartSampling();
}

/// <summary>
/// Periodically records process-level utilisation as <see cref="DataRefreshMetrics.RuntimeMetric"/>, for the duration
/// of a data refresh.
///
/// <para>
/// This is the measurement class the refresh has never had. A per-stage cost breakdown says where the time goes, but
/// not whether there is any headroom to recover it from: "stage 40 is 31% CPU / 69% DB" means "pipelining is free
/// money" at 25% CPU and "pipelining buys nothing, do less work or buy more cores" at 100% CPU, and cost alone cannot
/// tell those apart. It also settles, in passing, the thread-pool-starvation hypothesis for the Service Bus lock
/// losses, and prices the undated "BatchSize 4k throws OOM" folklore.
/// </para>
/// </summary>
public class DataRefreshRuntimeSampler : IDataRefreshRuntimeSampler
{
    /// <summary>
    /// 30s is deliberately coarse. The run is measured in hours, the metric is pre-aggregated anyway, and a tighter
    /// tick would add series volume without adding resolution to anything we intend to ask of it.
    /// </summary>
    private static readonly TimeSpan SamplingInterval = TimeSpan.FromSeconds(30);

    private const double BytesPerMb = 1024d * 1024d;

    private readonly IMatchingAlgorithmImportLogger logger;

    public DataRefreshRuntimeSampler(IMatchingAlgorithmImportLogger logger)
    {
        this.logger = logger;
    }

    public IAsyncDisposable StartSampling() => new SamplingSession(logger);

    /// <summary>
    /// Owns the sampling loop. The loop is a real, retained <see cref="Task"/> rather than an <c>async void</c> or a
    /// fire-and-forget: <see cref="DisposeAsync"/> cancels it and then awaits it, so the loop can never outlive the
    /// refresh, and any fault in it surfaces rather than being swallowed by the finalizer thread.
    /// </summary>
    private sealed class SamplingSession : IAsyncDisposable
    {
        private readonly IMatchingAlgorithmImportLogger logger;
        private readonly CancellationTokenSource cancellation = new();
        private readonly Task samplingLoop;

        private long previousTimestamp;
        private TimeSpan previousProcessorTime;
        private int previousGen2Collections;
        private TimeSpan previousGcPauseDuration;

        public SamplingSession(IMatchingAlgorithmImportLogger logger)
        {
            this.logger = logger;
            CaptureBaseline();
            samplingLoop = RunSamplingLoop(cancellation.Token);
        }

        public async ValueTask DisposeAsync()
        {
            await cancellation.CancelAsync();

            try
            {
                await samplingLoop;
            }
            catch (OperationCanceledException)
            {
                // Expected: this is how the loop ends.
            }
            finally
            {
                cancellation.Dispose();
            }
        }

        /// <summary>
        /// Every counter below is a delta against this baseline. Failing to take it must not fail the refresh, so a
        /// baseline that cannot be read simply leaves the first sample reading as if from process start.
        /// </summary>
        private void CaptureBaseline()
        {
            previousTimestamp = Stopwatch.GetTimestamp();

            try
            {
                previousProcessorTime = CurrentProcessorTime();
                previousGen2Collections = GC.CollectionCount(2);
                previousGcPauseDuration = GC.GetTotalPauseDuration();
            }
            catch (Exception e)
            {
                logger.SendTrace($"DATA REFRESH: runtime sampler failed to capture its baseline: {e}", LogLevel.Warn);
            }
        }

        private async Task RunSamplingLoop(CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(SamplingInterval);

            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                // A sampler must never be able to fail the job it is measuring. Anything thrown here (a Process
                // handle we cannot open in a locked-down host, say) is reported once and the loop carries on.
                try
                {
                    EmitSample();
                }
                catch (Exception e)
                {
                    logger.SendTrace($"DATA REFRESH: runtime sampler failed to take a sample: {e}", LogLevel.Warn);
                }
            }
        }

        private void EmitSample()
        {
            var timestamp = Stopwatch.GetTimestamp();
            var interval = Stopwatch.GetElapsedTime(previousTimestamp, timestamp);
            previousTimestamp = timestamp;

            var processorTime = CurrentProcessorTime();
            var gen2Collections = GC.CollectionCount(2);
            var gcPauseDuration = GC.GetTotalPauseDuration();

            if (interval > TimeSpan.Zero)
            {
                // Normalised across all cores, so 100% means "every core busy" rather than "one core busy". That is
                // the form the pipelining question needs: headroom is only headroom if there is a core free to use it.
                var cpuPercent = (processorTime - previousProcessorTime).TotalMilliseconds
                                 / (interval.TotalMilliseconds * Environment.ProcessorCount) * 100;
                SendCounter(DataRefreshMetrics.Counter_CpuPercent, cpuPercent);

                var gcPausePercent = (gcPauseDuration - previousGcPauseDuration).TotalMilliseconds
                                     / interval.TotalMilliseconds * 100;
                SendCounter(DataRefreshMetrics.Counter_GcPauseTimePercent, gcPausePercent);
            }

            // Deltas, not running totals: a running total renders as a meaningless ramp on the timechart, and the
            // question is "how much collection is this stage causing", which is a rate.
            SendCounter(DataRefreshMetrics.Counter_Gen2Collections, gen2Collections - previousGen2Collections);

            using (var process = Process.GetCurrentProcess())
            {
                SendCounter(DataRefreshMetrics.Counter_WorkingSetMb, process.WorkingSet64 / BytesPerMb);
            }

            SendCounter(DataRefreshMetrics.Counter_ThreadPoolQueueLength, ThreadPool.PendingWorkItemCount);
            SendCounter(DataRefreshMetrics.Counter_ThreadPoolThreadCount, ThreadPool.ThreadCount);

            previousProcessorTime = processorTime;
            previousGen2Collections = gen2Collections;
            previousGcPauseDuration = gcPauseDuration;
        }

        private void SendCounter(string counter, double value) =>
            logger.SendMetric(DataRefreshMetrics.RuntimeMetric, value, DataRefreshMetrics.RuntimeDims(counter));

        private static TimeSpan CurrentProcessorTime()
        {
            using var process = Process.GetCurrentProcess();
            return process.TotalProcessorTime;
        }
    }
}
