using System;
using System.Collections.Generic;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Test.Integration.Integration.Builders
{
    public class AlleleLevelMatchCriteriaBuilder
    {
        private readonly AlleleLevelMatchCriteria criteria;
        
        public AlleleLevelMatchCriteriaBuilder()
        {
            criteria = new AlleleLevelMatchCriteria
            {
                SearchType = DonorType.Adult,
                RegistriesToSearch = new List<RegistryCode> { RegistryCode.AN },
            };
        }
        
        public AlleleLevelMatchCriteriaBuilder WithTotalMismatchCount(int mismatchCount)
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
                    criteria.LocusMismatchDQB1 = locusMatchCriteria;
                    break;
                case Locus.Drb1:
                    criteria.LocusMismatchDRB1 = locusMatchCriteria;
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
            criteria.LocusMismatchDRB1 = criteria.LocusMismatchDRB1 ?? locusMatchCriteria;
            return this;
        }

        public AlleleLevelMatchCriteria Build()
        {
            return criteria;
        }
    }
}