using System;
using Atlas.SearchTracking.Common.Enums;

namespace Atlas.Debug.Client.Models.SearchTracking
{
    public class SearchTrackingMatchPredictionDetails
    {
        public int Id { get; set; }

        public int SearchRequestId { get; set; }

        public SearchTrackingSearchRequest SearchRequest { get; set; }

        public DateTime InitiationTimeUtc { get; set; }

        public DateTime StartTimeUtc { get; set; }

        public SearchTrackingTimingInfo PrepareBatchesTiming { get; set; }

        public SearchTrackingTimingInfo AlgorithmCoreRunningBatchesTiming { get; set; }

        public SearchTrackingTimingInfo PersistingResultsTiming { get; set; }
        
        public DateTime? CompletionTimeUtc { get; set; }

        public SearchTrackingFailureInfo FailureInfo { get; set; }

        public bool? IsSuccessful { get; set; }
    }
}