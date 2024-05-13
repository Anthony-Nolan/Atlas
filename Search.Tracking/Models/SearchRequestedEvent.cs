namespace Atlas.SearchTracking.Models
{
    public class SearchRequestedEvent
    {
        public Guid SearchRequestId { get; set; }
        public bool IsRepeatSearch { get; set; }
        public Guid? OriginalSearchRequestId { get; set; }
        public DateTime? RepeatSearchCutOffDate { get; set; }
        public string RequestJson { get; set; }
        public string SearchCriteria { get; set; }
        public string DonorType { get; set; }
        public DateTime RequestTimeUtc { get; set; }
    }
}
