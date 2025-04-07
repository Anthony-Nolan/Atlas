namespace Atlas.SearchTracking.Common.Models
{
    public class MatchPredictionTimingEvent
    {
        public Guid SearchIdentifier { get; set; }
        public DateTime TimeUtc { get; set; }
    }
}
