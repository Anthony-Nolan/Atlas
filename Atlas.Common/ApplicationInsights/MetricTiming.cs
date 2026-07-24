using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Atlas.Common.ApplicationInsights
{
    public static class AtlasLoggerMetricExtensions
    {
        /// <summary>
        /// Runs <paramref name="action"/>, recording its elapsed milliseconds as a pre-aggregated metric (see
        /// <see cref="TimeOperationAsMetric"/>). Convenience for the common "time an awaited call and return its
        /// result" case, where a bare <c>using</c> would force you to declare the result variable's type explicitly.
        /// </summary>
        public static async Task<T> RunTimedAsMetricAsync<T>(
            this IAtlasLogger logger,
            string metricName,
            Dictionary<string, string> dimensions,
            Func<Task<T>> action)
        {
            using (logger.TimeOperationAsMetric(metricName, dimensions))
            {
                return await action();
            }
        }

        /// <summary>
        /// Times the enclosing <c>using</c> block and, on dispose, records the elapsed milliseconds as a
        /// pre-aggregated Application Insights metric via <see cref="IAtlasLogger.SendMetric"/>.
        ///
        /// This is the metric-based replacement for the old <c>LongOperationLoggingStopwatch</c> /
        /// <c>LongStopwatchCollection</c> ecosystem, which logged timings as Trace text and had them silently
        /// sampled out. Metrics are never sampled, aggregate natively across threads (so parallel inner
        /// operations need no bespoke ThreadLocal/AsyncLocal bookkeeping), and land in the queryable
        /// <c>customMetrics</c> table with no message-text parsing required.
        ///
        /// Allocation-light: uses <see cref="Stopwatch.GetTimestamp"/> rather than allocating a Stopwatch per call.
        /// </summary>
        public static MetricOperationTimer TimeOperationAsMetric(
            this IAtlasLogger logger,
            string metricName,
            Dictionary<string, string> dimensions = null)
        {
            return new MetricOperationTimer(logger, metricName, dimensions);
        }
    }

    /// <summary>
    /// A readonly struct so that a <c>using</c> over the concrete type disposes without boxing.
    /// Do not assign it to an <see cref="IDisposable"/> variable in a hot path (that would box it).
    /// </summary>
    public readonly struct MetricOperationTimer : IDisposable
    {
        private readonly IAtlasLogger logger;
        private readonly string metricName;
        private readonly Dictionary<string, string> dimensions;
        private readonly long startTimestamp;

        internal MetricOperationTimer(IAtlasLogger logger, string metricName, Dictionary<string, string> dimensions)
        {
            this.logger = logger;
            this.metricName = metricName;
            this.dimensions = dimensions;
            startTimestamp = Stopwatch.GetTimestamp();
        }

        public void Dispose()
        {
            var elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
            logger.SendMetric(metricName, elapsedMs, dimensions);
        }
    }
}
