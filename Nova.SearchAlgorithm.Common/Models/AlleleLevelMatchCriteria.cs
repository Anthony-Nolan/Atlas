using System;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Common.Models
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
        
        public AlleleLevelLocusMatchCriteria MatchCriteriaForLocus(Locus locus)
        {
            switch (locus)
            {
                case Locus.A:
                    return LocusMismatchA;
                case Locus.B:
                    return LocusMismatchB;
                case Locus.C:
                    return LocusMismatchC;
                case Locus.Dqb1:
                    return LocusMismatchDQB1;
                case Locus.Drb1:
                    return LocusMismatchDRB1;
                default:
                    throw new NotImplementedException();
            }
        }
    }

    public class AlleleLevelLocusMatchCriteria
    {
        public int MismatchCount { get; set; }
        public IEnumerable<string> HlaNamesToMatchInPositionOne { get; set; }
        public IEnumerable<string> HlaNamesToMatchInPositionTwo { get; set; }
    }
}