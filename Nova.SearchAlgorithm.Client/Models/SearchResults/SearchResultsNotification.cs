namespace Nova.SearchAlgorithm.Client.Models.SearchResults
{
    public class SearchResultsNotification
    {
        public string SearchRequestId { get; set; }
        public bool WasSuccessful { get; set; }
        public int? NumberOfResults { get; set; }
    }
}