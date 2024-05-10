namespace Atlas.Search.Tracking.Models
{
    public class SearchRequestCompletedEvent
    {
        public int SearchRequestId { get; set; }
        public bool ResultsSent { get; set; }
        public DateTime ResultsSentTimeUTC { get; set; }
    }
}
