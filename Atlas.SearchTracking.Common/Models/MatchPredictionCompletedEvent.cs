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
        // The message of an HLA lookup error names the patient's HLA, and the stack trace has the message.
        [PatientInformation]
        public string? Message { get; set; }
        [PatientInformation]
        public string? ExceptionStacktrace { get; set; }
        public MatchPredictionFailureType? Type { get; set; }
    }
}
