using Atlas.MatchingAlgorithm.Client.Models;
using Atlas.MatchingAlgorithm.Common.Models;
using System;

namespace Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders
{
    public class AlleleLevelMatchCriteriaBuilder
    {
        private readonly AlleleLevelMatchCriteria criteria;
        
        public AlleleLevelMatchCriteriaBuilder()
        {
            criteria = new AlleleLevelMatchCriteria
            {
                SearchType = DonorType.Adult
            };
        }
        
        public AlleleLevelMatchCriteriaBuilder WithDonorMismatchCount(int mismatchCount)
        {
            criteria.DonorMismatchCount = mismatchCount;
            return this;
        }

        public AlleleLevelMatchCriteriaBuilder WithLocusMatchCriteria(Locus locus, AlleleLevelLocusMatchCriteria locusMatchCriteria)
        {
            switch (locus)
            {
                case Locus.A:
                    criteria.LocusMismatchA = locusMatchCriteria;
                    break;
                case Locus.B:
                    criteria.LocusMismatchB = locusMatchCriteria;
                    break;
                case Locus.C:
                    criteria.LocusMismatchC = locusMatchCriteria;
                    break;
                case Locus.Dpb1:
                    throw new NotImplementedException();
                case Locus.Dqb1:
                    criteria.LocusMismatchDqb1 = locusMatchCriteria;
                    break;
                case Locus.Drb1:
                    criteria.LocusMismatchDrb1 = locusMatchCriteria;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(locus), locus, null);
            }

            return this;
        }
        
        public AlleleLevelMatchCriteriaBuilder WithLocusMismatchCount(Locus locus, int mismatchCount)
        {
            switch (locus)
            {
                case Locus.A:
                    criteria.LocusMismatchA.MismatchCount = mismatchCount;
                    break;
                case Locus.B:
                    criteria.LocusMismatchB.MismatchCount = mismatchCount;
                    break;
                case Locus.C:
                    criteria.LocusMismatchC.MismatchCount = mismatchCount;
                    break;
                case Locus.Dpb1:
                    throw new NotImplementedException();
                case Locus.Dqb1:
                    criteria.LocusMismatchDqb1.MismatchCount = mismatchCount;
                    break;
                case Locus.Drb1:
                    criteria.LocusMismatchDrb1.MismatchCount = mismatchCount;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(locus), locus, null);
            }

            return this;
        }
        
        // Populates all null required match criteria (A, B, DRB) with given value
        public AlleleLevelMatchCriteriaBuilder WithDefaultLocusMatchCriteria(AlleleLevelLocusMatchCriteria locusMatchCriteria)
        {
            criteria.LocusMismatchA = criteria.LocusMismatchA ?? locusMatchCriteria;
            criteria.LocusMismatchB = criteria.LocusMismatchB ?? locusMatchCriteria;
            criteria.LocusMismatchDrb1 = criteria.LocusMismatchDrb1 ?? locusMatchCriteria;
            return this;
        }

        public AlleleLevelMatchCriteriaBuilder WithSearchType(DonorType searchType)
        {
            criteria.SearchType = searchType;
            return this;
        }
        
        public AlleleLevelMatchCriteria Build()
        {
            return criteria;
        }
    }
}