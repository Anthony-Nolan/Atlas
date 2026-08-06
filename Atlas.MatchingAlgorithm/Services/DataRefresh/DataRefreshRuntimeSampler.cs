using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
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
        private readonly SqlClientCounterListener sqlClientCounters;

        private long previousTimestamp;
        private TimeSpan previousProcessorTime;
        private int previousGen2Collections;
        private TimeSpan previousGcPauseDuration;

        public SamplingSession(IMatchingAlgorithmImportLogger logger)
        {
            this.logger = logger;
            CaptureBaseline();
            sqlClientCounters = StartSqlClientCounterListener(logger);
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
                // Disposed before the CTS so that no counter callback can outlive the session and emit into the next
                // refresh's series. An EventListener left attached to a long-lived Functions host would do exactly that.
                sqlClientCounters?.Dispose();
                cancellation.Dispose();
            }
        }

        /// <summary>
        /// Attaching the listener must never be able to fail the refresh - it is strictly a measurement - so a host
        /// that will not let us subscribe simply costs us the SQL counters and nothing else.
        /// </summary>
        private static SqlClientCounterListener StartSqlClientCounterListener(IMatchingAlgorithmImportLogger logger)
        {
            try
            {
                return new SqlClientCounterListener(logger);
            }
            catch (Exception e)
            {
                logger.SendTrace($"DATA REFRESH: could not attach the SqlClient counter listener: {e}", LogLevel.Warn);
                return null;
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

    /// <summary>
    /// Forwards a small, fixed subset of Microsoft.Data.SqlClient's own EventCounters into
    /// <see cref="DataRefreshMetrics.RuntimeMetric"/>, for the lifetime of one <see cref="SamplingSession"/>.
    ///
    /// <para>
    /// This answers a question no duration timer can. The per-locus bulk-insert prologue costs ~40 ms per call, which
    /// has been read as connection establishment - but a POOLED open costs microseconds; 40 ms is the shape of a
    /// PHYSICAL connect, or of a transaction enlistment. <c>hard-connects</c> against <c>soft-connects</c> distinguishes
    /// them directly, so the connection-reuse fix can be chosen (or dropped) on evidence rather than on arithmetic that
    /// merely happens to divide out.
    /// </para>
    /// </summary>
    private sealed class SqlClientCounterListener : EventListener
    {
        private const string SqlClientEventSourceName = "Microsoft.Data.SqlClient.EventSource";

        /// <summary>Matches the sampling loop's interval, so both halves of a sample line up on the same timeline.</summary>
        private const string CounterIntervalSeconds = "30";

        /// <summary>
        /// SqlClient publishes sixteen counters; these are the ones that bear on the question. Static, and read in
        /// <see cref="OnEventSourceCreated"/>, because that callback fires from the base constructor - BEFORE this
        /// class's own field initialisers run - so it must not touch instance state.
        /// </summary>
        private static readonly Dictionary<string, string> ForwardedCounters = new()
        {
            ["hard-connects"] = DataRefreshMetrics.Counter_SqlHardConnects,
            ["soft-connects"] = DataRefreshMetrics.Counter_SqlSoftConnects,
            ["number-of-non-pooled-connections"] = DataRefreshMetrics.Counter_SqlNonPooledConnections,
            ["number-of-active-connections"] = DataRefreshMetrics.Counter_SqlActiveConnections,
            ["number-of-free-connections"] = DataRefreshMetrics.Counter_SqlFreeConnections,
            ["number-of-stasis-connections"] = DataRefreshMetrics.Counter_SqlStasisConnections
        };

        private readonly IMatchingAlgorithmImportLogger logger;
        private bool hasReportedFailure;

        public SqlClientCounterListener(IMatchingAlgorithmImportLogger logger)
        {
            this.logger = logger;
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name != SqlClientEventSourceName)
            {
                return;
            }

            // EventKeywords.None is load-bearing. SqlClient's keywords turn on per-command execution traces and SNI
            // tracing; enabling them across a fifteen-hour run of tens of millions of commands would be ruinous, and
            // would perturb the very thing being measured. Counters arrive regardless of keywords, but ONLY if
            // EventCounterIntervalSec is supplied.
            EnableEvents(
                eventSource,
                EventLevel.Informational,
                EventKeywords.None,
                new Dictionary<string, string> { ["EventCounterIntervalSec"] = CounterIntervalSeconds });
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            // logger can legitimately be null here: EnableEvents above runs from the base constructor, so an event
            // could in principle arrive before this class's fields are assigned.
            if (logger == null || eventData.EventName != "EventCounters" || eventData.Payload == null)
            {
                return;
            }

            try
            {
                foreach (var payload in eventData.Payload)
                {
                    ForwardCounter(payload);
                }
            }
            catch (Exception e) when (!hasReportedFailure)
            {
                hasReportedFailure = true;
                logger.SendTrace($"DATA REFRESH: SqlClient counter listener failed to read a sample: {e}", LogLevel.Warn);
            }
            catch (Exception)
            {
                // Already reported once. A callback on the EventSource's timer must not throw, and must not flood.
            }
        }

        private void ForwardCounter(object payload)
        {
            if (payload is not IDictionary<string, object> counter
                || !counter.TryGetValue("Name", out var name)
                || name is not string counterName
                || !ForwardedCounters.TryGetValue(counterName, out var counterDimension))
            {
                return;
            }

            // Rate counters (hard-connects, soft-connects) report "Increment" - the delta over the interval, which is
            // the same delta-not-running-total convention the rest of this sampler uses. Polling counters report "Mean".
            if (TryReadValue(counter, "Increment", out var value) || TryReadValue(counter, "Mean", out value))
            {
                logger.SendMetric(DataRefreshMetrics.RuntimeMetric, value, DataRefreshMetrics.RuntimeDims(counterDimension));
            }
        }

        private static bool TryReadValue(IDictionary<string, object> counter, string key, out double value)
        {
            if (counter.TryGetValue(key, out var raw) && raw is double read)
            {
                value = read;
                return true;
            }

            value = 0;
            return false;
        }
    }
}
