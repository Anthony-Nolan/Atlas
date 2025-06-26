namespace Atlas.SearchTracking.Common.Models
{
    public interface ISearchTrackingMatchingAttemptEvent : ISearchTrackingEvent
    {
        byte AttemptNumber { get; set; }
    }
}