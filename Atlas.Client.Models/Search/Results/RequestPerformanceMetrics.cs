using System;

namespace Atlas.Client.Models.Search.Results
{
    /// <summary>
    /// Performance metrics related to request processing
    /// </summary>
    public class RequestPerformanceMetrics
    {
        /// <summary>
        /// Time that the request was received (equivalent to enqueued time for queued requests)
        /// </summary>
        public DateTimeOffset InitiationTime { get; set; }

        /// <summary>
        /// DateTime that the request was picked up for processing (equivalent to dequeued time for queued requests)
        /// </summary>
        public DateTimeOffset StartTime { get; set; }

        /// <summary>
        /// DateTime when request has completed processing
        /// </summary>
        public DateTimeOffset CompletionTime { get; set; }

        /// <summary>
        /// Time taken to complete the request (from <see cref="StartTime"/> to <see cref="CompletionTime"/>).
        /// </summary>
        public TimeSpan Duration => CompletionTime.Subtract(StartTime);
    }
}
