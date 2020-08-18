using System.Net;
using Atlas.Client.Models.Search.Results.Matching;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Models
{
    public class SearchAlgorithmApiResult
    {
        public HttpStatusCode StatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public MatchingAlgorithmResultSet Results { get; set; }
    }
}