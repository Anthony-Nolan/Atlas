using System;
using Atlas.Client.Models.Search.Requests;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Atlas.Client.Models.Search.Results.Matching
{
    public class MatchingResultsNotification : ResultsNotification
    {
        /// <summary>
        /// Include full search request details in results notification, as match prediction will need it to run,
        /// which is triggered by this notification.
        /// </summary>
        public SearchRequest SearchRequest { get; set; }

        public bool IsRepeatSearch => RepeatSearchRequestId != null;

        /// <summary>
        ///     The version of the deployed matching algorithm that ran the search request
        /// </summary>
        public string MatchingAlgorithmServiceVersion { get; set; }

        public TimeSpan ElapsedTime { get; set; }
    }
}