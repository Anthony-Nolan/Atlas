namespace Atlas.SearchTracking.Common.Models
{
    public class SearchRequestCompletedEvent
    {
        public Guid SearchIdentifier { get; set; }
        public bool ResultsSent { get; set; }
        public DateTime ResultsSentTimeUtc { get; set; }
    }
}
