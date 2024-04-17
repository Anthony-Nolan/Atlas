using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Models;

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

    public class MatchCriteriaBuilder 
    {
        private readonly AlleleLevelMatchCriteriaBuilder inner;

        public MatchCriteriaBuilder()
        {
            inner = new AlleleLevelMatchCriteriaBuilder();
        }


        public MatchCriteriaBuilder WithDonorMismatchCount(int mismatchCount)
        {
            inner.WithDonorMismatchCount(mismatchCount);
            return this;
        }

        public MatchCriteriaBuilder WithRequiredLociMatchCriteria(int mismatchCount)
        {
            inner.WithRequiredLociMatchCriteria(mismatchCount);
            return this;
        }

        public MatchCriteriaBuilder WithLocusMatchCriteria(Locus locus, AlleleLevelLocusMatchCriteria locusMatchCriteria)
        {
            inner.WithLocusMatchCriteria(locus, locusMatchCriteria);
            return this;
        }

        public MatchCriteriaBuilder WithLocusMismatchCount(Locus locus, int mismatchCount)
        {
            inner.WithLocusMismatchCount(locus, mismatchCount);
            return this;
        }

        public MatchCriteriaBuilder WithDefaultLocusMatchCriteria(AlleleLevelLocusMatchCriteria locusMatchCriteria)
        {
            inner.WithDefaultLocusMatchCriteria(locusMatchCriteria);
            return this;
        }

        public MatchCriteriaBuilder WithSearchType(DonorType searchType)
        {
            inner.WithSearchType(searchType);
            return this;
        }

        public MatchCriteriaBuilder WithShouldIncludeBetterMatches(bool shouldIncludeBetterMatches)        
        {
            inner.WithShouldIncludeBetterMatches(shouldIncludeBetterMatches);
            return this;
        }


        public MatchCriteria Build()
        {
            return new MatchCriteria
            {
                NonHlaFilteringCriteria = new NonHlaFilteringCriteria(),
                AlleleLevelMatchCriteria = inner.Build()
            };
        }
    }

}