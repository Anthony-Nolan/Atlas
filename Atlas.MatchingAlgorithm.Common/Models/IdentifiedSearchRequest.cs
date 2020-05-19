using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;

namespace Atlas.MatchingAlgorithm.Common.Models
{
    public class IdentifiedSearchRequest
    {
        public SearchRequest SearchRequest { get; set; }
        public string Id { get; set; }
    }
}