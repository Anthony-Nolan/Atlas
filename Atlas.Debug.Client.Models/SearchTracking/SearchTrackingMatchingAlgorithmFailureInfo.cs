namespace Atlas.Debug.Client.Models.SearchTracking
{
    public class SearchTrackingMatchingAlgorithmFailureInfo
    {
        public SearchTrackingMatchingAlgorithmFailureType? Type { get; set; }

        public string? Message { get; set; }

        public string? ExceptionStacktrace { get; set; }
    }
}