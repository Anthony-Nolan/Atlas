namespace Atlas.SearchTracking.Common.Models
{
    public class MatchPredictionResultsSentEvent
    {
        public Guid SearchIdentifier { get; set; }
        public DateTime TimeUtc { get; set; }
    }
}
