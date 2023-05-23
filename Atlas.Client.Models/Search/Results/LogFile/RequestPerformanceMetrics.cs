using System;

namespace Atlas.Client.Models.Search.Results.LogFile
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

        /// <summary>
        /// Time taken for the core step of the algorithm to complete.
        /// For matching, this will cover the main donor search step, but will exclude extra steps, like results batching and upload.
        /// For match prediction, this will cover the time taken to process match prediction requests for all donors, but will exclude any orchestration steps, such as downloading of matching results, or persisting of the final results.
        /// </summary>
        public TimeSpan? AlgorithmCoreStepDuration { get; set; }
    }
}
