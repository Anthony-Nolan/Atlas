using System.Collections.Generic;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Builders;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Repositories
{
    /// <summary>
    /// Generates and stores meta-donors for use in testing.
    /// </summary>
    public static class MetaDonorRepository
    {
        public static readonly IEnumerable<MetaDonor> MetaDonors = new List<MetaDonor>
        {
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                Registry = RegistryCode.AN,
                GenotypeCriteria = new GenotypeCriteriaBuilder().Build(),
                HlaTypingCategorySets = new List<PhenotypeInfo<HlaTypingCategory>>
                {
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingCategory(HlaTypingCategory.TgsFourFieldAllele).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingCategory(HlaTypingCategory.ThreeFieldTruncatedAllele).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingCategory(HlaTypingCategory.TwoFieldTruncatedAllele).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingCategory(HlaTypingCategory.XxCode).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingCategory(HlaTypingCategory.NmdpCode).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingCategory(HlaTypingCategory.Serology).Build(),
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