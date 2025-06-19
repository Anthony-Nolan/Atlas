using Atlas.SearchTracking.Common.Enums;

namespace Atlas.SearchTracking.Common.Models
{
    public class MatchPredictionCompletedEvent : ISearchTrackingEvent
    {
        public Guid SearchIdentifier { get; set; }
        public Guid? OriginalSearchIdentifier { get; set; }
        public DateTime CompletionTimeUtc { get; set; }
        public MatchPredictionCompletionDetails CompletionDetails { get; set; } = null!;
    }

    public class MatchPredictionCompletionDetails
    {
        public bool IsSuccessful { get; set; }
        public MatchPredictionFailureInfo? FailureInfo { get; set; }
        public int? DonorsPerBatch { get; set; }
        public int? TotalNumberOfBatches { get; set; }
    }

    public class MatchPredictionFailureInfo
    {
        public string? Message { get; set; }
        public string? ExceptionStacktrace { get; set; }
        public MatchPredictionFailureType? Type { get; set; }
    }
}
