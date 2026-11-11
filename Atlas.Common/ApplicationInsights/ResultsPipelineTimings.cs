using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Atlas.Common.ApplicationInsights
{
    /// <summary>
    /// Where one step of the results pipeline spends time and memory (ATL-526: find the bottlenecks in large search processing).
    /// Experiment only, not for release.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Begin"/> is called once per step (one function invocation), from the method that awaits the whole step. The
    /// collection flows through an <see cref="AsyncLocal{T}"/> into every method the step calls, so services and repositories
    /// add to it with the static methods and need no new dependencies. Disposing the collection sends one
    /// "Results pipeline timings" event, which <see cref="ResultsPipelineTimingsTelemetryInitializer"/> keeps out of sampling.
    /// </para>
    /// <para>
    /// Phase times are added up per name (<c>{name}Ms</c>, <c>{name}Calls</c>). Phases that run in parallel (for example blob
    /// downloads in <c>DownloadMultipleBlobs</c>) can add up to more than the wall-clock time.
    /// </para>
    /// <para>
    /// Memory numbers are process-wide. <c>ConcurrentStepsMax</c> says how many collections were running in this process at the
    /// same time; when it is more than 1, the memory numbers include other work.
    /// </para>
    /// <para>All the static methods do nothing when no collection is running (for example, in tests).</para>
    /// </remarks>
    public sealed class ResultsPipelineTimings : IDisposable
    {
        public const string EventName = "Results pipeline timings";

        private static readonly AsyncLocal<ResultsPipelineTimings> Current = new();
        private static int activeCollections;

        private readonly object sync = new();
        private readonly IAtlasLogger logger;
        private readonly ResultsPipelineTimings previous;
        private readonly Dictionary<string, string> props = new();
        private readonly Dictionary<string, double> metrics = new();
        private readonly long startTimestamp = Stopwatch.GetTimestamp();
        private readonly long allocatedBytesAtStart = GC.GetTotalAllocatedBytes();
        private readonly int gen2AtStart = GC.CollectionCount(2);
        private readonly Timer memorySampler;
        private long peakWorkingSet;
        private long peakGcHeap;
        private int concurrentStepsMax;
        private bool disposed;

        private ResultsPipelineTimings(IAtlasLogger logger, string step, ResultsPipelineTimings previous)
        {
            this.logger = logger;
            this.previous = previous;
            props["Step"] = step;
            props["Outcome"] = "Succeeded";

            var concurrent = Interlocked.Increment(ref activeCollections);
            concurrentStepsMax = concurrent;
            SampleMemory();
            memorySampler = new Timer(_ => SampleMemory(), null, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500));
        }

        /// <summary>
        /// Starts a collection for one step. Call it from the method that awaits the whole step, and dispose the result when the
        /// step ends.
        /// </summary>
        public static ResultsPipelineTimings Begin(
            IAtlasLogger logger,
            string step,
            string searchRequestId,
            string repeatSearchId = null,
            int? deliveryCount = null,
            DateTime? enqueuedTimeUtc = null)
        {
            var timings = new ResultsPipelineTimings(logger, step, Current.Value);
            timings.props["SearchRequestId"] = searchRequestId;
            timings.props["RepeatSearchId"] = repeatSearchId;

            if (deliveryCount != null)
            {
                timings.metrics["DeliveryCount"] = deliveryCount.Value;
            }

            if (enqueuedTimeUtc != null)
            {
                timings.metrics["MessageAgeAtStartMs"] = (DateTime.UtcNow - DateTime.SpecifyKind(enqueuedTimeUtc.Value, DateTimeKind.Utc)).TotalMilliseconds;
            }

            Current.Value = timings;
            return timings;
        }

        /// <summary>Times one phase. The time is added to <c>{name}Ms</c>, and <c>{name}Calls</c> goes up by one.</summary>
        public static Phase Time(string name) => new(Current.Value, name);

        /// <summary>Adds to a numeric value of the current step.</summary>
        public static void Add(string name, double value) => Current.Value?.AddMetric(name, value);

        /// <summary>Sets a numeric value of the current step.</summary>
        public static void SetMetric(string name, double value) => Current.Value?.SetMetricValue(name, value);

        /// <summary>Sets a text value of the current step. Never pass patient or donor data.</summary>
        public static void Set(string name, string value) => Current.Value?.SetProp(name, value);

        public static void AddBlobRead(long bytes)
        {
            Add("BlobReadFiles", 1);
            Add("BlobReadMb", bytes / 1024d / 1024d);
        }

        public static void AddBlobWritten(long bytes)
        {
            Add("BlobWrittenFiles", 1);
            Add("BlobWrittenMb", bytes / 1024d / 1024d);
        }

        /// <summary>Marks the step as failed. The exception message is not sent, only its type.</summary>
        public void Fail(Exception exception)
        {
            SetProp("Outcome", "Failed");
            SetProp("ExceptionType", exception.GetType().Name);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            memorySampler.Dispose();
            SampleMemory();
            Interlocked.Decrement(ref activeCollections);

            Dictionary<string, string> eventProps;
            Dictionary<string, double> eventMetrics;
            lock (sync)
            {
                metrics["TotalMs"] = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
                metrics["AllocatedMb"] = (GC.GetTotalAllocatedBytes() - allocatedBytesAtStart) / 1024d / 1024d;
                metrics["Gen2Collections"] = GC.CollectionCount(2) - gen2AtStart;
                metrics["WorkingSetEndMb"] = Environment.WorkingSet / 1024d / 1024d;
                metrics["PeakWorkingSetMb"] = peakWorkingSet / 1024d / 1024d;
                metrics["PeakGcHeapMb"] = peakGcHeap / 1024d / 1024d;
                metrics["ConcurrentStepsMax"] = concurrentStepsMax;
                eventProps = new Dictionary<string, string>(props);
                eventMetrics = new Dictionary<string, double>(metrics);
            }

            // Restore before sending, so a failing logger cannot leave this collection in place.
            if (ReferenceEquals(Current.Value, this))
            {
                Current.Value = previous;
            }

            logger.SendEvent(EventName, LogLevel.Info, eventProps, eventMetrics);
        }

        private void AddMetric(string name, double value)
        {
            lock (sync)
            {
                metrics[name] = metrics.TryGetValue(name, out var existing) ? existing + value : value;
            }
        }

        private void SetMetricValue(string name, double value)
        {
            lock (sync)
            {
                metrics[name] = value;
            }
        }

        private void SetProp(string name, string value)
        {
            lock (sync)
            {
                props[name] = value;
            }
        }

        private void SampleMemory()
        {
            var workingSet = Environment.WorkingSet;
            var gcHeap = GC.GetTotalMemory(false);
            var concurrent = Volatile.Read(ref activeCollections);
            lock (sync)
            {
                peakWorkingSet = Math.Max(peakWorkingSet, workingSet);
                peakGcHeap = Math.Max(peakGcHeap, gcHeap);
                concurrentStepsMax = Math.Max(concurrentStepsMax, concurrent);
            }
        }

        public readonly struct Phase : IDisposable
        {
            private readonly ResultsPipelineTimings timings;
            private readonly string name;
            private readonly long start;

            internal Phase(ResultsPipelineTimings timings, string name)
            {
                this.timings = timings;
                this.name = name;
                start = timings == null ? 0 : Stopwatch.GetTimestamp();
            }

            public void Dispose()
            {
                if (timings == null)
                {
                    return;
                }

                timings.AddMetric($"{name}Ms", Stopwatch.GetElapsedTime(start).TotalMilliseconds);
                timings.AddMetric($"{name}Calls", 1);
            }
        }
    }
}
