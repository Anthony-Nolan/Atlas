using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models.Donors;

namespace Atlas.MatchingAlgorithm.Common.Models
{
    public class AlleleLevelMatchCriteria
    {
        public DonorType SearchType { get; set; }

        public int DonorMismatchCount { get; set; }
        
        public bool ShouldIncludeBetterMatches { get; set; }

        public LociInfo<AlleleLevelLocusMatchCriteria> LocusCriteria { get; set; } = new LociInfo<AlleleLevelLocusMatchCriteria>();

        public IEnumerable<Locus> LociWithCriteriaSpecified() =>
            LocusCriteria.Map((l, c) => c == null ? (Locus?) null : l)
                .ToEnumerable()
                .Where(x => x != null)
                .Select(x => x.Value);

        public override string ToString()
            => $"{LocusCriteria.A?.MismatchCount}{LocusCriteria.B?.MismatchCount}{LocusCriteria.Drb1?.MismatchCount}{LocusCriteria.C?.MismatchCount}{LocusCriteria.Dqb1?.MismatchCount}";
    }

    public class AlleleLevelLocusMatchCriteria
    {
        public int MismatchCount { get; set; }
        public IEnumerable<string> PGroupsToMatchInPositionOne { get; set; }
        public IEnumerable<string> PGroupsToMatchInPositionTwo { get; set; }
    }
}