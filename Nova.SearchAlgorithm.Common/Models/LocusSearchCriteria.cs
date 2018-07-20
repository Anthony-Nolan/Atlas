using System.Collections.Generic;
using Nova.SearchAlgorithm.Client.Models;

namespace Nova.SearchAlgorithm.Common.Models
{
    public class LocusSearchCriteria
    {
        public DonorType SearchType { get; set; }
        public IEnumerable<RegistryCode> Registries { get; set; }
        public IEnumerable<string> PGroupsToMatchInPositionOne { get; set; }
        public IEnumerable<string> PGroupsToMatchInPositionTwo { get; set; }
    }
}
