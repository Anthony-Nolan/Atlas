using Nova.SearchAlgorithm.Client.Models;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Models
{
    // TODO:NOVA-931 this merely duplicates SearchRequest, maybe we don't need it?
    public class SearchCriteria
    {
        public SearchType SearchType { get; set; }
        public IEnumerable<RegistryCode> Registries { get; set; }
        public FiveLociDetails<SingleLocusDetails<MatchingHla>> LocusMatchCriteria {get; set;}
    }
}
