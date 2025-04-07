namespace Atlas.SearchTracking.Common.Models
{
    public class MatchingAlgorithmAttemptTimingEvent
    {
        public Guid SearchIdentifier { get; set; }
        public byte AttemptNumber { get; set; }
        public DateTime TimeUtc { get; set; }
    }
}
