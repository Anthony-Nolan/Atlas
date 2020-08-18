using System;

namespace Atlas.Functions.PublicApi.Models.Search.Results
{
    public class SearchResultsNotification
    {
        public string SearchRequestId { get; set; }
        public bool WasSuccessful { get; set; }
        public int? NumberOfResults { get; set; }
        /// <summary>
        /// The version of the HLA Nomenclature used to run the search request - used for analysing both donor and patient hla.
        /// </summary>
        public string HlaNomenclatureVersion { get; set; }
        public string BlobStorageContainerName { get; set; }
        public string ResultsFileName { get; set; }
        
        /// <summary>
        /// If the search was not a success, this should be populated to indicate which stage of search failed. 
        /// </summary>
        public string FailureMessage { get; set; }
        
        /// <summary>
        /// Time taken to run the matching algorithm - currently includes matching, and scoring.
        /// </summary>
        public TimeSpan MatchingAlgorithmTime { get; set; }
        
        /// <summary>
        /// Total time taken to run the match prediction algorithm for all results.
        ///
        /// Note that this can run in parallel - the logged time is the time between starting running MPA requests, and getting the last results.
        /// The sum of all MPA processing time may exceed this, if donors were calculated in parallel.
        /// </summary>
        public TimeSpan MatchPredictionTime { get; set; }
        
        /// <summary>
        /// Total time between search initiation and results notification.
        /// 
        /// Will exceed the sum of matching algorithm and match prediction, as this time also includes:
        ///     - Fetching donor metadata to use in the match prediction algorithm
        ///     - Conversion of search results
        ///     - Persisting results to Azure storage
        ///     - Any other plumbing / orchestration time.
        /// </summary>
        public TimeSpan OverallSearchTime { get; set; }
    }
}