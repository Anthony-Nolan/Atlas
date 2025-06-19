namespace Atlas.SearchTracking.Common.Models
{
    public class MatchPredictionTimingEvent : ISearchTrackingEvent
    {
        public Guid SearchIdentifier { get; set; }
        public Guid? OriginalSearchIdentifier { get; set; }
        public DateTime TimeUtc { get; set; }
    }
}
