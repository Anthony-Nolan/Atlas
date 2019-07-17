namespace Nova.SearchAlgorithm.Client.Models.SearchResults
{
    public class SearchResultsNotification
    {
        public string SearchRequestId { get; set; }
        public bool WasSuccessful { get; set; }
        public int? NumberOfResults { get; set; }
        /// <summary>
        /// The version of the deployed search algorithm that ran the search request
        /// </summary>
        public string SearchAlgorithmServiceVersion { get; set; }
        /// <summary>
        /// The version of the hla database used to run the search request - used for analysing both donor and patient hla.
        /// </summary>
        public string WmdaHlaDatabaseVersion { get; set; }
        public string BlobStorageContainerName { get; set; }
        public long SearchTimeInMilliseconds { get; set; }
    }
}