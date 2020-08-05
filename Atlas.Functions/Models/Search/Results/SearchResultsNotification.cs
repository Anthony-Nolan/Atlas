using System;

namespace Atlas.Functions.Models.Search.Results
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
        
        public TimeSpan MatchingAlgorithmTime { get; set; }
        public TimeSpan MatchPredictionTime { get; set; }
    }
}