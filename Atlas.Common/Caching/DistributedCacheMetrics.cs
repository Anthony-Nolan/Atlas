using System;
using Microsoft.ApplicationInsights;

namespace Atlas.Common.Caching
{
    internal enum DistributedCacheOperation
    {
        Get,
        Set,
        Remove
    }

    internal enum DistributedCacheResult
    {
        /// <summary>A read found a value.</summary>
        Hit,

        /// <summary>A read reached the cache, and found no value.</summary>
        Miss,

        /// <summary>A write reached the cache.</summary>
        Succeeded,

        /// <summary>The call went to the cache, and failed.</summary>
        Failed,

        /// <summary>The circuit was open, so the call did not go to the cache.</summary>
        Skipped
    }

    /// <summary>
    /// The measurements of the distributed cache, for each cache name - see <see cref="DistributedCacheKey.CacheName"/>.
    /// </summary>
    /// <remarks>
    /// Pre-aggregated Application Insights metrics, not events or traces: the telemetry of a worker is sampled, and a
    /// metric is not. Query: <c>customMetrics | where name startswith "DistributedCache."</c>, split by
    /// <c>customDimensions.Cache</c>, <c>customDimensions.Operation</c> and <c>customDimensions.Result</c>. The hit ratio of
    /// a cache is its count of <c>Get</c> calls with the result <c>Hit</c>, divided by its count of all <c>Get</c> calls.
    /// For the latency of a cache, use only the results <c>Hit</c>, <c>Miss</c> and <c>Succeeded</c>: a <c>Skipped</c>
    /// call, and a <c>Failed</c> call while there is no connection, take almost no time.
    /// </remarks>
    internal interface IDistributedCacheMetrics
    {
        void RecordOperation(string cacheName, DistributedCacheOperation operation, DistributedCacheResult result, TimeSpan duration);

        /// <summary>The size of a value that a read found, or that a write stored.</summary>
        void RecordValueSize(string cacheName, DistributedCacheOperation operation, int bytes);
    }

    internal sealed class DistributedCacheMetrics : IDistributedCacheMetrics
    {
        internal const string DurationMetricName = "DistributedCache.DurationMs";
        internal const string ValueBytesMetricName = "DistributedCache.ValueBytes";

        // A metric id must always be used with the same dimensions.
        private const string CacheDimension = "Cache";
        private const string OperationDimension = "Operation";
        private const string ResultDimension = "Result";

        private readonly TelemetryClient telemetryClient;

        public DistributedCacheMetrics(TelemetryClient telemetryClient)
        {
            this.telemetryClient = telemetryClient;
        }

        /// <inheritdoc />
        public void RecordOperation(string cacheName, DistributedCacheOperation operation, DistributedCacheResult result, TimeSpan duration)
        {
            telemetryClient.GetMetric(DurationMetricName, CacheDimension, OperationDimension, ResultDimension)
                .TrackValue(duration.TotalMilliseconds, cacheName, operation.ToString(), result.ToString());
        }

        /// <inheritdoc />
        public void RecordValueSize(string cacheName, DistributedCacheOperation operation, int bytes)
        {
            telemetryClient.GetMetric(ValueBytesMetricName, CacheDimension, OperationDimension)
                .TrackValue(bytes, cacheName, operation.ToString());
        }
    }
}
