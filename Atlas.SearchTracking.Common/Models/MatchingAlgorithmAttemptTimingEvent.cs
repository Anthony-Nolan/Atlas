namespace Atlas.SearchTracking.Common.Models
{
    public class MatchingAlgorithmAttemptTimingEvent : ISearchTrackingMatchingAttemptEvent
    {
        public Guid SearchIdentifier { get; set; }
        public Guid? OriginalSearchIdentifier { get; set; }
        public byte AttemptNumber { get; set; }
        public DateTime TimeUtc { get; set; }
    }
}