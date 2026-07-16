using System;
using System.Collections.Generic;

namespace Atlas.ManualTesting.Models
{
    /// <summary>
    /// Request for the one-time utility that replays searches which ran on the parallel ("Containers") match-prediction
    /// path and ended in a failed or incomplete state, re-dispatching them with <c>ParallelMatchPrediction = false</c>
    /// so they take the legacy sequential Durable orchestrator path.
    /// </summary>
    public class ParallelMatchPredictionReplayRequest
    {
        /// <summary>Inclusive lower bound (UTC) on the original <c>[SearchTracking].[SearchRequests].RequestTimeUtc</c>.</summary>
        public DateTime FromRequestTimeUtc { get; set; }

        /// <summary>Inclusive upper bound (UTC) on the original <c>[SearchTracking].[SearchRequests].RequestTimeUtc</c>.</summary>
        public DateTime ToRequestTimeUtc { get; set; }

        /// <summary>
        /// When <c>true</c> (the default), the utility only returns the matching candidate searches and does NOT
        /// re-dispatch anything. Review the returned list, then call again with <c>DryRun = false</c> to actually replay.
        /// </summary>
        public bool DryRun { get; set; } = true;

        /// <summary>
        /// Optional allow-list of search identifiers to replay. When null or empty, every candidate in the window is
        /// replayed (only when <c>DryRun = false</c>). When provided, only candidates whose identifier appears here
        /// are replayed — identifiers not present in the candidate set are ignored.
        /// </summary>
        public List<Guid> SearchIdentifiers { get; set; }
    }
}
