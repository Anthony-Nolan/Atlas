namespace Atlas.SearchTracking.Models
{
    public class MatchPredictionStartedEvent
    {
        public int SearchRequestId { get; set; }
        public DateTime InitiationTimeUtc { get; set; }
        public DateTime StartTimeUtc { get; set; }
    }
}
