namespace Atlas.Debug.Client.Models.SearchTracking
{
    public class SearchTrackingMatchPredictionFailureInfo
    {
        public SearchTrackingMatchPredictionFailureType? Type { get; set; }

        public string? Message { get; set; }

        public string? ExceptionStacktrace { get; set; }
    }
}
