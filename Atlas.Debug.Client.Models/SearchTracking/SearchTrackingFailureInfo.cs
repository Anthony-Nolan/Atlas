using Atlas.SearchTracking.Common.Enums;

namespace Atlas.Debug.Client.Models.SearchTracking
{
    public class SearchTrackingFailureInfo
    {
        public MatchingAlgorithmFailureType? Type { get; set; }

        public string? Message { get; set; }

        public string? ExceptionStacktrace { get; set; }
    }
}