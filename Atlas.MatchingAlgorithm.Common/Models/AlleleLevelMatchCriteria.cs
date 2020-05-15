using System;
using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Client.Models;

namespace Atlas.MatchingAlgorithm.Common.Models
{
    public class AlleleLevelMatchCriteria
    {
        public DonorType SearchType { get; set; }

        public int DonorMismatchCount { get; set; }

        public AlleleLevelLocusMatchCriteria LocusMismatchA { get; set; }
        public AlleleLevelLocusMatchCriteria LocusMismatchB { get; set; }
        public AlleleLevelLocusMatchCriteria LocusMismatchC { get; set; }
        public AlleleLevelLocusMatchCriteria LocusMismatchDqb1 { get; set; }
        public AlleleLevelLocusMatchCriteria LocusMismatchDrb1 { get; set; }
        
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
                    return LocusMismatchDqb1;
                case Locus.Drb1:
                    return LocusMismatchDrb1;
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
            if (LocusMismatchDrb1 != null)
            {
                loci.Add(Locus.Drb1);
            }
            if (LocusMismatchDqb1 != null)
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