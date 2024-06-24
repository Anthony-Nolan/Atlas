namespace Atlas.SearchTracking.Common.Models
{
    public class MatchingAlgorithmAttemptStartedEvent
    {
        public int SearchRequestId { get; set; }
        public byte AttemptNumber { get; set; }
        public DateTime InitiationTimeUtc { get; set; }
        public DateTime StartTimeUtc { get; set; }
    }
}
