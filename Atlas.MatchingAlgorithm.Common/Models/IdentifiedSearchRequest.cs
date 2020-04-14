using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;

namespace Atlas.MatchingAlgorithm.Models
{
    public class IdentifiedSearchRequest
    {
        public SearchRequest SearchRequest { get; set; }
        public string Id { get; set; }
    }
}