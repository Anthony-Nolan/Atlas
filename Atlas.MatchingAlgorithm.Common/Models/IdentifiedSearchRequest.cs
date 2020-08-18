using Atlas.Client.Models.Search.Requests;

namespace Atlas.MatchingAlgorithm.Common.Models
{
    public class IdentifiedSearchRequest
    {
        public SearchRequest MatchingRequest { get; set; }
        public string Id { get; set; }
    }
}