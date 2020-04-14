using System.Net;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Models
{
    public class SearchAlgorithmApiResult
    {
        public HttpStatusCode StatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public SearchResultSet Results { get; set; }
    }
}