namespace Atlas.SearchTracking.Common.Models
{
    public interface ISearchTrackingEvent
    {
        Guid SearchIdentifier { get; set; }

        Guid? OriginalSearchIdentifier { get; set; }
    }
}