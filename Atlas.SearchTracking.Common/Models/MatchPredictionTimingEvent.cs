namespace Atlas.SearchTracking.Common.Models
{
    public class MatchPredictionTimingEvent
    {
        public Guid SearchRequestId { get; set; }
        public DateTime TimeUtc { get; set; }
    }
}
