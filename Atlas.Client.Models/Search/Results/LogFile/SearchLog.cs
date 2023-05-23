namespace Atlas.Client.Models.Search.Results.LogFile
{
    public class SearchLog
    {
        public string SearchRequestId { get; set; }

        /// <summary>
        /// Did request complete successfully?
        /// </summary>
        public bool WasSuccessful { get; set; }

        /// <summary>
        /// Performance metrics captured for the request.
        /// </summary>
        public RequestPerformanceMetrics RequestPerformanceMetrics { get; set; }
    }
}