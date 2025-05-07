using System.Collections.Generic;
using System;

namespace Atlas.Debug.Client.Models.SearchTracking
{
    public class SearchTrackingSearchRequest
    {
        public int Id { get; set; }

        public Guid SearchIdentifier { get; set; }

        public bool IsRepeatSearch { get; set; }

        public Guid? OriginalSearchIdentifier { get; set; }

        public DateTime? RepeatSearchCutOffDate { get; set; }

        public string RequestJson { get; set; }

        public string SearchCriteria { get; set; }

        public string DonorType { get; set; }

        public bool AreBetterMatchesIncluded { get; set; }

        public List<string>? DonorRegistryCodes { get; set; }

        public DateTime RequestTimeUtc { get; set; }

        public SearchTrackingMatchingAlgorithmInfo SearchTrackingMatchingAlgorithmInfo { get; set; }

        public bool IsMatchPredictionRun { get; set; }

        public SearchTrackingMatchPredictionInfo SearchTrackingMatchPredictionInfo { get; set; }

        public bool? ResultsSent { get; set; }

        public DateTime? ResultsSentTimeUtc { get; set; }

        public SearchTrackingMatchPredictionDetails? SearchRequestMatchPredictionDetails { get; set; }

        public List<SearchTrackingMatchingAlgorithmAttemptDetails> SearchRequestMatchingAlgorithmAttemptDetails { get; set; }
    }
}