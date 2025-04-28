using System;

namespace Atlas.Debug.Client.Models.SearchTracking
{
    public class SearchTrackingMatchingAlgorithmAttemptDetails
    {
        public int Id { get; set; }

        public int SearchRequestId { get; set; }

        public SearchTrackingSearchRequest SearchRequest { get; set; }

        public byte AttemptNumber { get; set; }

        public DateTime InitiationTimeUtc { get; set; }

        public DateTime StartTimeUtc { get; set; }

        public SearchTrackingTimingInfo AlgorithmCoreMatchingTiming { get; set; }

        public SearchTrackingTimingInfo AlgorithmCoreScoringTiming { get; set; }

        public SearchTrackingTimingInfo PersistingResultsTiming { get; set; }

        public DateTime? CompletionTimeUtc { get; set; }

        public bool? IsSuccessful { get; set; }

        public SearchTrackingFailureInfo FailureInfo { get; set; }
    }
}