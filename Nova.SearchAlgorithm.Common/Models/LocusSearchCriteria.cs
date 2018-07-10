using Nova.SearchAlgorithm.Common.Models;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Repositories.Donors
{
    public class LocusSearchCriteria
    {
        public DonorType SearchType { get; set; }
        public IEnumerable<RegistryCode> Registries { get; set; }
        public IEnumerable<string> PGroupsToMatchInPositionOne { get; set; }
        public IEnumerable<string> PGroupsToMatchInPositionTwo { get; set; }
    }
}
