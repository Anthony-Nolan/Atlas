using System.Net;
using Nova.SearchAlgorithm.Client.Models.SearchResults;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Models
{
    public class SearchAlgorithmApiResult
    {
        public HttpStatusCode StatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public SearchResultSet Results { get; set; }
    }
}