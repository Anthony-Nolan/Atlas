using System.Collections.Generic;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Builders;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Resources
{
    /// <summary>
    /// This class contains the static test data for the 'meta-donors'.
    /// Each 'meta-donor' corresponds to a donor with fully TGS typed HLA (i.e. Genotype)
    ///
    /// This underlying genotype will be 'dumbed down' to lower resolutions. Each meta-donor specifies a list of resolutions -
    /// each entry in this list will correspond to a donor in the database, which will share general donor information (e.g. type, registry),
    /// with HLA according at the resolutions specified.
    ///
    /// When adding new tests, it is likely that new meta donors and/or resolutions for existing meta-donors will need to be added.
    /// The tests should fail with an appropriate error message if no suitable meta-donor was found - if this happens new donors should be added here
    /// </summary>
    public static class MetaDonorsData
    {
        public static readonly IEnumerable<MetaDonor> MetaDonors = new List<MetaDonor>
        {
            // TGS donors with an arbitrary field count (2, 3, or 4)
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
            },

            // Two field tgs donors
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                Registry = RegistryCode.AN,
                GenotypeCriteria = new GenotypeCriteriaBuilder()
                    .WithTgsTypingCategoryAtAllLoci(TgsHlaTypingCategory.TwoFieldAllele)
                    .Build(),
            },

            // P Group matching
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                Registry = RegistryCode.AN,
                GenotypeCriteria = new GenotypeCriteriaBuilder().WithPGroupMatchPossibleAtAllLoci().Build(),
            },

            // G Group matching
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                Registry = RegistryCode.AN,
                GenotypeCriteria = new GenotypeCriteriaBuilder().WithGGroupMatchPossibleAtAllLoci().Build(),
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