using System.Collections.Generic;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Data.Models
{
    public class AlleleLevelMatchCriteria
    {
        public DonorType SearchType { get; set; }
        public IEnumerable<RegistryCode> RegistriesToSearch { get; set; }

        public int DonorMismatchCount { get; set; }

        public AlleleLevelLocusMatchCriteria LocusMismatchA { get; set; }
        public AlleleLevelLocusMatchCriteria LocusMismatchB { get; set; }
        public AlleleLevelLocusMatchCriteria LocusMismatchC { get; set; }
        public AlleleLevelLocusMatchCriteria LocusMismatchDQB1 { get; set; }
        public AlleleLevelLocusMatchCriteria LocusMismatchDRB1 { get; set; }
    }

    public class AlleleLevelLocusMatchCriteria
    {
        public int MismatchCount { get; set; }
        public IEnumerable<string> HlaNamesToMatchInPositionOne { get; set; }
        public IEnumerable<string> HlaNamesToMatchInPositionTwo { get; set; }
    }
}