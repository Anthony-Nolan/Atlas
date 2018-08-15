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
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                Registry = RegistryCode.AN,
                GenotypeCriteria = new GenotypeCriteriaBuilder().Build(),
                HlaTypingCategorySets = new List<PhenotypeInfo<HlaTypingResolution>>
                {
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingCategory(HlaTypingResolution.Tgs).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingCategory(HlaTypingResolution.ThreeFieldTruncatedAllele).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingCategory(HlaTypingResolution.TwoFieldTruncatedAllele).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingCategory(HlaTypingResolution.XxCode).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingCategory(HlaTypingResolution.NmdpCode).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingCategory(HlaTypingResolution.Serology).Build(),
                    new HlaTypingCategorySetBuilder().UntypedAtLocus(Locus.C).Build(),
                    new HlaTypingCategorySetBuilder().UntypedAtLocus(Locus.Dqb1).Build(),
                    new HlaTypingCategorySetBuilder().UntypedAtLocus(Locus.C).UntypedAtLocus(Locus.Dqb1).Build(),
                }
            },
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                Registry = RegistryCode.AN,
                GenotypeCriteria = new GenotypeCriteriaBuilder()
                    .WithTgsTypingCategoryAtAllLoci(TgsHlaTypingCategory.ThreeFieldAllele)
                    .Build(),
                HlaTypingCategorySets = new List<PhenotypeInfo<HlaTypingResolution>>
                {
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingCategory(HlaTypingResolution.Tgs).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingCategory(HlaTypingResolution.TwoFieldTruncatedAllele).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingCategory(HlaTypingResolution.XxCode).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingCategory(HlaTypingResolution.NmdpCode).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingCategory(HlaTypingResolution.Serology).Build(),
                    new HlaTypingCategorySetBuilder().UntypedAtLocus(Locus.C).Build(),
                    new HlaTypingCategorySetBuilder().UntypedAtLocus(Locus.Dqb1).Build(),
                    new HlaTypingCategorySetBuilder().UntypedAtLocus(Locus.C).UntypedAtLocus(Locus.Dqb1).Build(),
                }
            },
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                Registry = RegistryCode.AN,
                GenotypeCriteria = new GenotypeCriteriaBuilder()
                    .WithTgsTypingCategoryAtAllLoci(TgsHlaTypingCategory.TwoFieldAllele)
                    .Build(),
                HlaTypingCategorySets = new List<PhenotypeInfo<HlaTypingResolution>>
                {
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingCategory(HlaTypingResolution.Tgs).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingCategory(HlaTypingResolution.XxCode).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingCategory(HlaTypingResolution.NmdpCode).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingCategory(HlaTypingResolution.Serology).Build(),
                    new HlaTypingCategorySetBuilder().UntypedAtLocus(Locus.C).Build(),
                    new HlaTypingCategorySetBuilder().UntypedAtLocus(Locus.Dqb1).Build(),
                    new HlaTypingCategorySetBuilder().UntypedAtLocus(Locus.C).UntypedAtLocus(Locus.Dqb1).Build(),
                }
            },
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                Registry = RegistryCode.AN,
                GenotypeCriteria = new GenotypeCriteriaBuilder().WithNonUniquePGroupsAtAllLoci().Build()
            },
            new MetaDonor
            {
                DonorType = DonorType.Cord,
                Registry = RegistryCode.AN,
                GenotypeCriteria = new GenotypeCriteriaBuilder().Build(),
            },
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                Registry = RegistryCode.DKMS,
                GenotypeCriteria = new GenotypeCriteriaBuilder().Build(),
            },
            new MetaDonor
            {
                DonorType = DonorType.Cord,
                Registry = RegistryCode.DKMS,
                GenotypeCriteria = new GenotypeCriteriaBuilder().Build(),
            }
        };
    }
}