using System;

namespace Atlas.ManualTesting.Models
{
    public class WmdaParallelRunPerformanceInfo
    {
        public string SearchRequestId { get; set; }
        public bool WasSuccessful { get; set; }
        public int? DonorCount { get; set; }
        public TimeSpan? MatchingQueueDuration { get; set; }
        public TimeSpan? MatchingRequestDuration { get; set; }
        public TimeSpan? MatchPredictionQueueDuration { get; set; }
        public TimeSpan? MatchPredictionRequestDuration { get; set; }
        public DateTimeOffset? MatchingInitiationTime { get; set; }
        public DateTimeOffset? MatchPredictionCompletionTime { get; set; }
    }
}
