namespace Atlas.SearchTracking.Common.Models
{
    public class MatchingAlgorithmAttemptTimingEvent
    {
        public int SearchRequestId { get; set; }
        public byte AttemptNumber { get; set; }
        public DateTime TimeUtc { get; set; }
    }
}
