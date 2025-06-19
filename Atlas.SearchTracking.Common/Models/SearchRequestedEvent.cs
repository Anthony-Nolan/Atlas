namespace Atlas.SearchTracking.Common.Models
{
    public class SearchRequestedEvent : ISearchTrackingEvent
    {
        public Guid SearchIdentifier { get; set; }
        public bool IsRepeatSearch { get; set; }
        public Guid? OriginalSearchIdentifier { get; set; }
        public DateTime? RepeatSearchCutOffDate { get; set; }
        public string RequestJson { get; set; }
        public string SearchCriteria { get; set; }
        public string DonorType { get; set; }
        public DateTime RequestTimeUtc { get; set; }
        public bool IsMatchPredictionRun { get; set; }
        public bool AreBetterMatchesIncluded { get; set; }
        public ICollection<string> DonorRegistryCodes { get; set; }
    }
}
