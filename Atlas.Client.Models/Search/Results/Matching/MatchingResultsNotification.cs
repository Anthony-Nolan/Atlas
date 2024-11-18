using System;
using Atlas.Client.Models.Common.Requests;


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
        /// The version of the deployed matching algorithm that ran the search request
        /// </summary>
        public string MatchingAlgorithmServiceVersion { get; set; }

        public TimeSpan ElapsedTime { get; set; }

        /// <summary>
        /// <inheritdoc cref="MatchingAlgorithmFailureInfo.ValidationError"/>
        /// </summary>
        [Obsolete(message:$"Superseded by {nameof(FailureInfo)}. Will only be set for backwards compatibility.")]
        public string ValidationError { get; set; }

        /// <summary>
        /// <inheritdoc cref="MatchingAlgorithmFailureInfo"/>
        /// Will only be set in the event of a failure (i.e. <see cref="ResultsNotification.WasSuccessful"/> is `false`).
        /// </summary>
        public MatchingAlgorithmFailureInfo FailureInfo { get; set; }
    }

    /// <summary>
    /// Information related to a matching failure.
    /// </summary>
    public class MatchingAlgorithmFailureInfo
    {
        /// <summary>
        /// Will be only be set if search failed due to a validation error.
        /// Most validation errors will be caught at search initiation time, and the search will not get queued in the first place.
        /// However, some "validation" exceptions can occur at search runtime - notably HLA values that look reasonable, but are not recognised in
        /// the current HLA nomenclature.
        ///
        /// Such errors will be specified here, to let consumers know that the issue with the search lies with the input HLA.
        /// All other, unexpected, errors will not be detailed here - we do not want to expose internal error details to consumers, so AI logs must
        /// be used to diagnose other such issues. 
        /// </summary>
        public string ValidationError { get; set; }

        /// <summary>
        /// Number of times this request has been processed so far, including the current run.
        /// I.e., On the first attempt, <see cref="AttemptNumber"/> will have a value of 1.
        /// </summary>
        public int AttemptNumber { get; set; }

        /// <summary>
        /// Number of retries remaining: if the value is 0, this request will not be automatically re-run, and can be considered "permanently" failed.
        /// </summary>
        public int RemainingRetriesCount { get; set; }
    }
}