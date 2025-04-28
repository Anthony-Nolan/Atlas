using Atlas.SearchTracking.Common.Enums;

namespace Atlas.Debug.Client.Models.SearchTracking
{
    public class SearchTrackingMatchPredictionInfo
    {
        public bool? IsSuccessful { get; set; }

        public string? FailureInfo_Message { get; set; }

        public string? FailureInfo_ExceptionStacktrace { get; set; }

        public MatchPredictionFailureType? FailureInfo_Type { get; set; }

        public int? DonorsPerBatch { get; set; }

        public int? TotalNumberOfBatches { get; set; }
    }
}
