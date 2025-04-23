using System;
using Atlas.SearchTracking.Common.Enums;

namespace Atlas.Debug.Client.Models.SearchTracking
{
    public class SearchRequestMatchPrediction
    {
        public int Id { get; set; }

        public int SearchRequestId { get; set; }

        public SearchRequest SearchRequest { get; set; }

        public DateTime InitiationTimeUtc { get; set; }

        public DateTime StartTimeUtc { get; set; }

        public DateTime? PrepareBatches_StartTimeUtc { get; set; }

        public DateTime? PrepareBatches_EndTimeUtc { get; set; }

        public DateTime? AlgorithmCore_RunningBatches_StartTimeUtc { get; set; }

        public DateTime? AlgorithmCore_RunningBatches_EndTimeUtc { get; set; }

        public DateTime? PersistingResults_StartTimeUtc { get; set; }

        public DateTime? PersistingResults_EndTimeUtc { get; set; }

        public DateTime? CompletionTimeUtc { get; set; }

        public MatchPredictionFailureType? FailureInfo_Type { get; set; }

        public string? FailureInfo_Message { get; set; }

        public string? FailureInfo_ExceptionStacktrace { get; set; }

        public bool? IsSuccessful { get; set; }
    }
}