using System;

namespace Atlas.Debug.Client.Models.SearchTracking
{
    public class SearchTrackingMatchingAlgorithmInfo
    {
        public bool? IsSuccessful { get; set; }

        public SearchTrackingMatchingAlgorithmFailureInfo FailureInfo { get; set; }

        public byte? TotalAttemptsNumber { get; set; }

        public int? NumberOfResults { get; set; }

        public RepeatSearchMatchingAlgorithmDetails RepeatDetails { get; set; }

        public string? HlaNomenclatureVersion { get; set; }

        public bool? ResultsSent { get; set; }

        public DateTime? ResultsSentTimeUtc { get; set; }
    }
}
