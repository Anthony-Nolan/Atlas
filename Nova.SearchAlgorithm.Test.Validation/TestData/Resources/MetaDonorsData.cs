using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Builders;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Resources
{
    public interface IMetaDonorsData
    {
        IEnumerable<MetaDonor> MetaDonors { get; }
    }
    
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
    public class MetaDonorsData: IMetaDonorsData
    {
        /// <summary>
        /// The number of meta-donors to create when testing a range of donors
        /// </summary>
        private const int DonorRangeCount = 50;

        /// <summary>
        /// Individual meta-donors. The majority of test data will be in here
        /// </summary>
        private static readonly IEnumerable<MetaDonor> IndividualMetaDonors = new List<MetaDonor>
        {
            // TGS donors with an arbitrary field count (2, 3, or 4)
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                Registry = RegistryCode.AN,
                GenotypeCriteria = new GenotypeCriteriaBuilder().WithAlleleStringContainingDifferentGroupsAtAllLoci().Build(),
                HlaTypingResolutionSets = new List<PhenotypeInfo<HlaTypingResolution>>
                {
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.Tgs).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.XxCode).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.NmdpCode).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.Serology).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.AlleleStringOfNames).Build(),
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
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.Arbitrary).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.XxCode).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.NmdpCode).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.Serology).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.ThreeFieldTruncatedAllele).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.TwoFieldTruncatedAllele).Build(),
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.AlleleStringOfNames).Build(),
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

            // Allele string of subtypes
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                Registry = RegistryCode.AN,
                GenotypeCriteria = new GenotypeCriteriaBuilder().WithAlleleStringOfSubtypesPossibleAtAllLoci().Build(),
                HlaTypingResolutionSets = new List<PhenotypeInfo<HlaTypingResolution>>
                {
                    new HlaTypingCategorySetBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.AlleleStringOfSubtypes).Build(),
                }
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
            
            // Three field matching (fourth field difference)
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                Registry = RegistryCode.AN,
                GenotypeCriteria = new GenotypeCriteriaBuilder()
                    .WithThreeFieldMatchPossibleAtAllLoci()
                    .Build(),
            },
            
            // Two field matching (third field difference)
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                Registry = RegistryCode.AN,
                GenotypeCriteria = new GenotypeCriteriaBuilder()
                    .WithTwoFieldMatchPossibleAtAllLoci()
                    .Build(),
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

            // Adult at NHSBT
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                Registry = RegistryCode.NHSBT,
                GenotypeCriteria = new GenotypeCriteriaBuilder().Build(),
            },

            // Adult at WBS
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                Registry = RegistryCode.WBS,
                GenotypeCriteria = new GenotypeCriteriaBuilder().Build(),
            },
        };

        /// <summary>
        /// Used when testing a selection of four-field TGS typed meta-donors
        /// </summary>
        private static readonly IEnumerable<MetaDonor> FourFieldDonorRange = Enumerable.Range(0, DonorRangeCount + 1).Select(i => new MetaDonor
        {
            DonorType = DonorType.Adult,
            Registry = RegistryCode.AN,
            GenotypeCriteria = new GenotypeCriteriaBuilder().WithTgsTypingCategoryAtAllLoci(TgsHlaTypingCategory.FourFieldAllele).Build(),
        });

        /// <summary>
        /// Used when testing a selection of three-field TGS typed meta-donors
        /// </summary>
        private static readonly IEnumerable<MetaDonor> ThreeFieldDonorRange = Enumerable.Range(0, DonorRangeCount + 1).Select(i => new MetaDonor
        {
            DonorType = DonorType.Adult,
            Registry = RegistryCode.AN,
            GenotypeCriteria = new GenotypeCriteriaBuilder().WithTgsTypingCategoryAtAllLoci(TgsHlaTypingCategory.ThreeFieldAllele).Build(),
        });

        /// <summary>
        /// Used when testing a selection of two-field TGS meta-donors
        /// </summary>
        private static readonly IEnumerable<MetaDonor> TwoFieldDonorRange = Enumerable.Range(0, DonorRangeCount + 1).Select(i => new MetaDonor
        {
            DonorType = DonorType.Adult,
            Registry = RegistryCode.AN,
            GenotypeCriteria = new GenotypeCriteriaBuilder().WithTgsTypingCategoryAtAllLoci(TgsHlaTypingCategory.TwoFieldAllele).Build(),
        });

        /// <summary>
        /// Used when testing a selection of TGS meta-donors with an arbitrary number of fields
        /// </summary>
        private static readonly IEnumerable<MetaDonor> ArbitraryFieldCountDonorRange = Enumerable.Range(0, DonorRangeCount + 1).Select(i => new MetaDonor
        {
            DonorType = DonorType.Adult,
            Registry = RegistryCode.AN,
            GenotypeCriteria = new GenotypeCriteriaBuilder().WithTgsTypingCategoryAtAllLoci(TgsHlaTypingCategory.Arbitrary).Build(),
        });

        public IEnumerable<MetaDonor> MetaDonors
        {
            get => IndividualMetaDonors
                .Concat(FourFieldDonorRange)
                .Concat(ThreeFieldDonorRange)
                .Concat(TwoFieldDonorRange)
                .Concat(ArbitraryFieldCountDonorRange);
        }
    }
}