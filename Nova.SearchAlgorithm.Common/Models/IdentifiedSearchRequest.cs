using Nova.SearchAlgorithm.Client.Models.SearchRequests;

namespace Nova.SearchAlgorithm.Models
{
    public class IdentifiedSearchRequest
    {
        public SearchRequest SearchRequest { get; set; }
        public string Id { get; set; }
    }
}