using Atlas.Client.Models.Search.Results.Matching;
using System;
using System.Collections.Generic;

namespace Atlas.Functions.Models
{
    /// <summary>
    /// Parameters wrapped in single object as Azure Activity functions may only have one parameter.
    /// </summary>
    public class PersistSearchResultsFunctionParameters
    {
        public MatchingResultsNotification MatchingResultsNotification { get; set; }

        /// <summary>
        /// Keyed by ATLAS Donor ID
        /// </summary>
        public TimedResultSet<IReadOnlyDictionary<int, string>> MatchPredictionResultLocations { get; set; }

        /// <summary>
        /// The time the *orchestration function* was initiated. Used to calculate an overall search time for Atlas search requests.
        /// </summary>
        public DateTime SearchInitiated { get; set; }

        public DateTimeOffset SearchStartTime { get; set; }
    }
}
