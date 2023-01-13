using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Common.Models;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders
{
    public class AlleleLevelMatchCriteriaBuilder
    {
        private readonly AlleleLevelMatchCriteria criteria;

        public AlleleLevelMatchCriteriaBuilder()
        {
            criteria = new AlleleLevelMatchCriteria
            {
                LocusCriteria = new LociInfo<AlleleLevelLocusMatchCriteria>(),
                SearchType = DonorType.Adult
            };
        }

        public AlleleLevelMatchCriteriaBuilder WithDonorMismatchCount(int mismatchCount)
        {
            criteria.DonorMismatchCount = mismatchCount;
            return this;
        }

        public AlleleLevelMatchCriteriaBuilder WithRequiredLociMatchCriteria(int mismatchCount)
        {
            criteria.LocusCriteria = criteria.LocusCriteria.SetLocus(Locus.A, new AlleleLevelLocusMatchCriteria {MismatchCount = mismatchCount});
            criteria.LocusCriteria = criteria.LocusCriteria.SetLocus(Locus.B, new AlleleLevelLocusMatchCriteria {MismatchCount = mismatchCount});
            criteria.LocusCriteria = criteria.LocusCriteria.SetLocus(Locus.Drb1, new AlleleLevelLocusMatchCriteria {MismatchCount = mismatchCount});
            return this;
        }

        public AlleleLevelMatchCriteriaBuilder WithLocusMatchCriteria(Locus locus, AlleleLevelLocusMatchCriteria locusMatchCriteria)
        {
            criteria.LocusCriteria = criteria.LocusCriteria.SetLocus(locus, locusMatchCriteria);
            return this;
        }

        public AlleleLevelMatchCriteriaBuilder WithLocusMismatchCount(Locus locus, int mismatchCount)
        {
            var locusCriteria = criteria.LocusCriteria.GetLocus(locus) ?? new AlleleLevelLocusMatchCriteria();
            locusCriteria.MismatchCount = mismatchCount;
            return WithLocusMatchCriteria(locus, locusCriteria);
        }

        // Populates all null required match criteria (A, B, DRB) with given value
        public AlleleLevelMatchCriteriaBuilder WithDefaultLocusMatchCriteria(AlleleLevelLocusMatchCriteria locusMatchCriteria)
        {
            var requiredLoci = new[] {Locus.A, Locus.B, Locus.Drb1};
            criteria.LocusCriteria = criteria.LocusCriteria.Map((l, v) => requiredLoci.Contains(l) ? v ?? locusMatchCriteria : v);
            return this;
        }

        public AlleleLevelMatchCriteriaBuilder WithSearchType(DonorType searchType)
        {
            criteria.SearchType = searchType;
            return this;
        }

        public AlleleLevelMatchCriteriaBuilder WithShouldIncludeBetterMatches(bool shouldIncludeBetterMatches)
        {
            criteria.ShouldIncludeBetterMatches = shouldIncludeBetterMatches;
            return this;
        }

        public AlleleLevelMatchCriteria Build()
        {
            return criteria;
        }
    }
}