using System;

namespace Nova.SearchAlgorithm.Client.Models
{
    // TODO:NOVA-924 define what a search result needs to include
    public class SearchResult
    {
        public int SearchRequestId { get; set; }
        public DonorMatch DonorMatch { get; set; }
    }
}