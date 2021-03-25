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

        /// <summary>
        ///     In the case of an original search this ID will be null
        /// </summary>
        public string RepeatSearchRequestId { get; set; }

        public bool IsRepeatSearch => RepeatSearchRequestId != null;

        /// <summary>
        ///     The version of the deployed matching algorithm that ran the search request
        /// </summary>
        public string MatchingAlgorithmServiceVersion { get; set; }

        /// <summary>
        ///     The version of the HLA Nomenclature used to run the search request - used for analysing both donor and patient hla.
        /// </summary>
        public string HlaNomenclatureVersion { get; set; }

        public TimeSpan ElapsedTime { get; set; }
    }
}