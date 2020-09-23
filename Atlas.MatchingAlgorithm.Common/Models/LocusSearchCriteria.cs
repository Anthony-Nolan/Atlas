using System.Collections.Generic;
using Atlas.MatchingAlgorithm.Client.Models.Donors;

namespace Atlas.MatchingAlgorithm.Common.Models
{
    public class LocusSearchCriteria
    {
        public DonorType SearchDonorType { get; set; }
        public IEnumerable<int> PGroupIdsToMatchInPositionOne { get; set; }
        public IEnumerable<int> PGroupIdsToMatchInPositionTwo { get; set; }
        public int MismatchCount { get; set; }
    }
}
