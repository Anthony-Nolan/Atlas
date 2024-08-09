namespace Atlas.SearchTracking.Common.Models
{
    public class SearchRequestCompletedEvent
    {
        public Guid SearchRequestId { get; set; }
        public bool ResultsSent { get; set; }
        public DateTime ResultsSentTimeUtc { get; set; }
    }
}
