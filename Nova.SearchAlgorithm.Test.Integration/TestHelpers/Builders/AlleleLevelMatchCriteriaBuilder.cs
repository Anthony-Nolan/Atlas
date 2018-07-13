using System;
using System.Collections.Generic;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders
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
                    criteria.LocusMismatchDQB1.MismatchCount = mismatchCount;
                    break;
                case Locus.Drb1:
                    criteria.LocusMismatchDRB1.MismatchCount = mismatchCount;
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

        public AlleleLevelMatchCriteriaBuilder WithSearchType(DonorType searchType)
        {
            criteria.SearchType = searchType;
            return this;
        }
        
        public AlleleLevelMatchCriteriaBuilder WithRegistries(IEnumerable<RegistryCode> registries)
        {
            criteria.RegistriesToSearch = registries;
            return this;
        }
        
        public AlleleLevelMatchCriteriaBuilder WithDonorMismatchCount(int donorMismatchCount)
        {
            criteria.DonorMismatchCount = donorMismatchCount;
            return this;
        }
        
        public AlleleLevelMatchCriteria Build()
        {
            return criteria;
        }
    }
}