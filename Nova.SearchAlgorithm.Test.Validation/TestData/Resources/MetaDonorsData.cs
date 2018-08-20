using System.Collections.Generic;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Builders;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Resources
{
    public static class MetaDonorsData
    {
        public static readonly IEnumerable<MetaDonor> MetaDonors = new List<MetaDonor>
        {
            // Arbitrary length tgs donors
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                Registry = RegistryCode.AN,
                GenotypeCriteria = new GenotypeCriteriaBuilder().Build(),
                HlaTypingResolutionSets = new List<PhenotypeInfo<HlaTypingResolution>>
                {
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.Tgs).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.XxCode).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.NmdpCode).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.Serology).Build(),
                    new HlaTypingCategorySetBuilder().UntypedAtLocus(Locus.C).Build(),
                    new HlaTypingCategorySetBuilder().UntypedAtLocus(Locus.Dqb1).Build(),
                    new HlaTypingCategorySetBuilder().UntypedAtLocus(Locus.C).UntypedAtLocus(Locus.Dqb1).Build(),
                }
            },

            // Four field tgs donors
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                Registry = RegistryCode.AN,
                GenotypeCriteria = new GenotypeCriteriaBuilder()
                    .WithTgsTypingCategoryAtAllLoci(TgsHlaTypingCategory.FourFieldAllele)
                    .Build(),
                HlaTypingResolutionSets = new List<PhenotypeInfo<HlaTypingResolution>>
                {
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.Tgs).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.ThreeFieldTruncatedAllele).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.TwoFieldTruncatedAllele).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.NmdpCode).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.Serology).Build(),
                    new HlaTypingCategorySetBuilder().WithDifferentlyTypedLoci().Build(),
                }
            },

            // Three field tgs donors
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                Registry = RegistryCode.AN,
                GenotypeCriteria = new GenotypeCriteriaBuilder()
                    .WithTgsTypingCategoryAtAllLoci(TgsHlaTypingCategory.ThreeFieldAllele)
                    .Build(),
                HlaTypingResolutionSets = new List<PhenotypeInfo<HlaTypingResolution>>
                {
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.Tgs).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.TwoFieldTruncatedAllele).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.NmdpCode).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.Serology).Build(),
                }
            },

            // Two field tgs donors
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                Registry = RegistryCode.AN,
                GenotypeCriteria = new GenotypeCriteriaBuilder()
                    .WithTgsTypingCategoryAtAllLoci(TgsHlaTypingCategory.TwoFieldAllele)
                    .Build(),
                HlaTypingResolutionSets = new List<PhenotypeInfo<HlaTypingResolution>>
                {
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.Tgs).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.NmdpCode).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.Serology).Build(),
                    new HlaTypingCategorySetBuilder().UntypedAtLocus(Locus.C).Build(),
                    new HlaTypingCategorySetBuilder().UntypedAtLocus(Locus.Dqb1).Build(),
                    new HlaTypingCategorySetBuilder().UntypedAtLocus(Locus.C).UntypedAtLocus(Locus.Dqb1).Build(),
                }
            },

            // P Group matching
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                Registry = RegistryCode.AN,
                GenotypeCriteria = new GenotypeCriteriaBuilder().WithPGroupMatchPossibleAtAllLoci().Build(),
                HlaTypingResolutionSets = new List<PhenotypeInfo<HlaTypingResolution>>
                {
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.Tgs).Build()
                }
            },

            // G Group matching
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                Registry = RegistryCode.AN,
                GenotypeCriteria = new GenotypeCriteriaBuilder().WithGGroupMatchPossibleAtAllLoci().Build(),
                HlaTypingResolutionSets = new List<PhenotypeInfo<HlaTypingResolution>>
                {
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.Tgs).Build()
                }
            },

            // Homozygous at A
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                Registry = RegistryCode.AN,
                GenotypeCriteria = new GenotypeCriteriaBuilder().HomozygousAtLocus(Locus.A).Build(),
                HlaTypingResolutionSets = new List<PhenotypeInfo<HlaTypingResolution>>
                {
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.Tgs).Build()
                }
            },
            
            // Cord
            new MetaDonor
            {
                DonorType = DonorType.Cord,
                Registry = RegistryCode.AN,
                GenotypeCriteria = new GenotypeCriteriaBuilder().Build(),
            },

            // Adult at DKMS
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                Registry = RegistryCode.DKMS,
                GenotypeCriteria = new GenotypeCriteriaBuilder().Build(),
            },

            // Cord at DKMS
            new MetaDonor
            {
                DonorType = DonorType.Cord,
                Registry = RegistryCode.DKMS,
                GenotypeCriteria = new GenotypeCriteriaBuilder().Build(),
            },
        };
    }
}