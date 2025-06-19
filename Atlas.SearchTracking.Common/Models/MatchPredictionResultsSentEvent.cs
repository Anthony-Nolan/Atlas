namespace Atlas.SearchTracking.Common.Models
{
    public class MatchPredictionResultsSentEvent : ISearchTrackingEvent
    {
        public Guid SearchIdentifier { get; set; }
        public Guid? OriginalSearchIdentifier { get; set; }
        public DateTime TimeUtc { get; set; }
    }
}
