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
        
        /// <summary>
        /// Will be null if the search succeeded (i.e. <see cref="ResultsNotification.WasSuccessful"/> is true).
        /// Most validation errors will be caught at search initiation time, and the search will not get queued in the first place.
        /// However, some "validation" exceptions can occur at search runtime - notably HLA values that look reasonable, but are not recognised in
        /// the current HLA nomenclature.
        ///
        /// Such errors will be specified here, to let consumers know that the issue with the search lies with the input HLA.
        /// All other, unexpected, errors will not be detailed here - we do not want to expose internal error details to consumers, so AI logs must
        /// be used to diagnose other such issues. 
        /// </summary> 
        public string ValidationError { get; set; }
    }
}