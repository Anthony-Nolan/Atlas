using System.Collections.Generic;
using Atlas.Client.Models.Search.Results;

namespace Atlas.ManualTesting.Models
{
    public class PeekedSearchResultsNotifications
    {
        public int TotalNotificationCount { get; set; }
        public int UniqueSearchRequestIdCount { get; set; }
        public int WasSuccessfulCount { get; set; }
        public int WasNotSuccessfulCount => TotalNotificationCount - WasSuccessfulCount;
        public IDictionary<string, int> FailureInfo { get; set; }
        public SearchTimes SearchTimesInSeconds { get; set; }
        public IEnumerable<SearchResultsNotification> PeekedNotifications { get; set; }
    }

    public class SearchTimes
    {
        public SearchTimesInSeconds MatchingAlgorithm { get; set; }
        public SearchTimesInSeconds MatchPrediction { get; set; }
        public SearchTimesInSeconds Overall { get; set; }
    }

    public class SearchTimesInSeconds
    {
        public double Mean { get; set; }
        public double Min { get; set; }
        public double Median { get; set; }
        public double Max { get; set; }
        public IEnumerable<double> AllTimings { get; set; }
    }
}
