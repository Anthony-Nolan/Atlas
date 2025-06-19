namespace Atlas.SearchTracking.Common.Models
{
    public class SearchRequestCompletedEvent : ISearchTrackingEvent
    {
        public Guid SearchIdentifier { get; set; }
        public Guid? OriginalSearchIdentifier { get; set; }
        public bool ResultsSent { get; set; }
        public DateTime ResultsSentTimeUtc { get; set; }
    }
}
