namespace Atlas.SearchTracking.Common.Models
{
    public class SearchRequestCompletedEvent
    {
        public int SearchRequestId { get; set; }
        public bool ResultsSent { get; set; }
        public DateTime ResultsSentTimeUtc { get; set; }
    }
}
