using Atlas.Debug.Client.Models.Enums;

namespace Atlas.Debug.Client.Models.SearchTracking
{
    public class SearchTrackingMatchingAlgorithmFailureInfo
    {
        public MatchingAlgorithmFailureType? Type { get; set; }

        public string? Message { get; set; }

        public string? ExceptionStacktrace { get; set; }
    }
}