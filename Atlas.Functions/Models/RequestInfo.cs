namespace Atlas.Functions.Models
{
    public class RequestInfo
    {
        public string Stage { get; set; }
        public string SearchRequestId { get; set; }
        public string RepeatSearchRequestId { get; set; }

        /// <summary>
        /// Will only have a value in the event that the matching algorithm request failed due to a validation error.
        /// </summary>
        public string ValidationError { get; set; }
    }
}
