using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models.Donors;

namespace Atlas.MatchingAlgorithm.Common.Models
{
    public class AlleleLevelMatchCriteria
    {
        public DonorType SearchType { get; set; }

        public int DonorMismatchCount { get; set; }

        public LociInfo<AlleleLevelLocusMatchCriteria> LocusCriteria { get; set; } = new LociInfo<AlleleLevelLocusMatchCriteria>();

        public IEnumerable<Locus> LociWithCriteriaSpecified()
        {
            var loci = new List<Locus>();
            if (LocusCriteria.A != null)
            {
                loci.Add(Locus.A);
            }
            if (LocusCriteria.B != null)
            {
                loci.Add(Locus.B);
            }
            if (LocusCriteria.C != null)
            {
                loci.Add(Locus.C);
            }
            if (LocusCriteria.Drb1 != null)
            {
                loci.Add(Locus.Drb1);
            }
            if (LocusCriteria.Dqb1 != null)
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