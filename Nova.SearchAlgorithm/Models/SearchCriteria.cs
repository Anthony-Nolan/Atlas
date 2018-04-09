using Nova.SearchAlgorithm.Client.Models;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Models
{
    public class SearchCriteria
    {
        public SearchType SearchType { get; set; }
        public IEnumerable<RegistryCode> Registries { get; set; }
        public FiveLociDetails<MatchingHla> LocusMatchCriteria {get; set;}
    }
}
