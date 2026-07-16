using System;
using System.Collections.Generic;

namespace Atlas.ManualTesting.Models
{
    /// <summary>
    /// Result of a call to the parallel match-prediction replay utility. On a dry run only <see cref="Candidates"/> is
    /// populated; on a real run <see cref="Replays"/> reports the outcome of each attempted re-dispatch.
    /// </summary>
    public class ParallelMatchPredictionReplayResponse
    {
        public bool DryRun { get; set; }

        /// <summary>Number of failed/incomplete parallel searches found in the requested time window.</summary>
        public int CandidateCount { get; set; }

        /// <summary>Number of searches successfully re-dispatched (always 0 on a dry run).</summary>
        public int ReplayedCount { get; set; }

        /// <summary>Number of searches that were attempted but failed to re-dispatch (always 0 on a dry run).</summary>
        public int FailedToReplayCount { get; set; }

        public IReadOnlyCollection<ParallelMatchPredictionReplayCandidate> Candidates { get; set; }

        public IReadOnlyCollection<ParallelMatchPredictionReplayOutcome> Replays { get; set; }
    }

    /// <summary>A failed/incomplete parallel search that is eligible for replay.</summary>
    public class ParallelMatchPredictionReplayCandidate
    {
        public Guid SearchIdentifier { get; set; }
        public bool IsRepeatSearch { get; set; }
        public Guid? OriginalSearchIdentifier { get; set; }
        public DateTime RequestTimeUtc { get; set; }

        /// <summary><c>false</c> = the match-prediction run failed; <c>null</c> = it never completed (incomplete).</summary>
        public bool? MatchPredictionIsSuccessful { get; set; }

        public string MatchPredictionFailureType { get; set; }

        public bool ResultsSent { get; set; }
    }

    /// <summary>The outcome of a single replay attempt.</summary>
    public class ParallelMatchPredictionReplayOutcome
    {
        /// <summary>The identifier of the original failed/incomplete search that was replayed.</summary>
        public Guid OriginalSearchIdentifier { get; set; }

        public bool IsRepeatSearch { get; set; }

        public bool WasReplayed { get; set; }

        /// <summary>The identifier of the newly-dispatched (repeat) search, when the replay succeeded.</summary>
        public string NewSearchIdentifier { get; set; }

        /// <summary>Populated when the replay failed (deserialization error, validation failure, HTTP error, etc.).</summary>
        public string Error { get; set; }
    }
}
