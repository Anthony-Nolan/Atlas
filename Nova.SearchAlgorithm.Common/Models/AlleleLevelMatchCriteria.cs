using System;
using System.Collections.Generic;
using Nova.SearchAlgorithm.Client.Models;

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

        public IEnumerable<Locus> LociWithCriteriaSpecified()
        {
            var loci = new List<Locus>();
            if (LocusMismatchA != null)
            {
                loci.Add(Locus.A);
            }
            if (LocusMismatchB != null)
            {
                loci.Add(Locus.B);
            }
            if (LocusMismatchC != null)
            {
                loci.Add(Locus.C);
            }
            if (LocusMismatchDRB1 != null)
            {
                loci.Add(Locus.Drb1);
            }
            if (LocusMismatchDQB1 != null)
            {
                loci.Add(Locus.Dqb1);
            }

            return loci;
        }
    }

    public class AlleleLevelLocusMatchCriteria
    {
        public int MismatchCount { get; set; }
        public IEnumerable<string> PGroupsToMatchInPositionOne { get; set; }
        public IEnumerable<string> PGroupsToMatchInPositionTwo { get; set; }
    }
}