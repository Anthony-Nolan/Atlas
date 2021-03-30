using System.Net;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.ResultSet;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Models
{
    public class SearchAlgorithmApiResult
    {
        public HttpStatusCode StatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public ResultSet<MatchingAlgorithmResult> Results { get; set; }
    }
}