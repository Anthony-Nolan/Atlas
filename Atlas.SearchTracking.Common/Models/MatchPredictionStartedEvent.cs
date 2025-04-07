namespace Atlas.SearchTracking.Common.Models
{
    public class MatchPredictionStartedEvent
    {
        public Guid SearchIdentifier { get; set; }
        public DateTime InitiationTimeUtc { get; set; }
        public DateTime StartTimeUtc { get; set; }
    }
}
