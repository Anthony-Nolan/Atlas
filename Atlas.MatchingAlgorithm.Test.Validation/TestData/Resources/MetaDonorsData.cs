using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Builders.Criteria;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.PatientDataSelection;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Resources
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
    /// each entry in this list will correspond to a donor in the database, which will share general donor information (e.g. type),
    /// with HLA according at the resolutions specified.
    ///
    /// When adding new tests, it is likely that new meta donors and/or resolutions for existing meta-donors will need to be added.
    /// The tests should fail with an appropriate error message if no suitable meta-donor was found - if this happens new donors should be added here
    /// </summary>
    public class MetaDonorsData : IMetaDonorsData
    {
        /// <summary>
        /// The number of meta-donors to create when testing a small range of donors
        /// </summary>
        private const int SmallDonorRangeCount = 10;

        /// <summary>
        /// The number of meta-donors to create when testing a large range of donors
        /// </summary>
        private const int LargeDonorRangeCount = 100;

        /// <summary>
        /// Individual meta-donors. The majority of test data will be in here
        /// </summary>
        private static readonly IEnumerable<MetaDonor> IndividualMetaDonors = new List<MetaDonor>
        {
            // TGS donors with an arbitrary field count (2, 3, or 4)
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                GenotypeCriteria = new GenotypeCriteriaBuilder().WithAlleleStringContainingDifferentGroupsAtAllLoci().Build(),
                DatabaseDonorSpecifications = new List<DatabaseDonorSpecification>
                {
                    new DatabaseDonorSelectionCriteriaBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.Tgs).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.XxCode).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.NmdpCode).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.Serology).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.PGroup).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.GGroup).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.AlleleStringOfNames).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder().UntypedAtLocus(Locus.C).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder().UntypedAtLocus(Locus.Dqb1).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder().UntypedAtLocus(Locus.C).UntypedAtLocus(Locus.Dqb1).Build(),
                }
            },

            // Four field tgs donors
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                GenotypeCriteria = new GenotypeCriteriaBuilder()
                    .WithTgsTypingCategoryAtAllLoci(TgsHlaTypingCategory.FourFieldAllele)
                    .Build(),
                DatabaseDonorSpecifications = new List<DatabaseDonorSpecification>
                {
                    new DatabaseDonorSelectionCriteriaBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.Tgs).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.Arbitrary).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.XxCode).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.NmdpCode).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.Serology).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.PGroup).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.GGroup).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.ThreeFieldTruncatedAllele).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.TwoFieldTruncatedAllele).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.AlleleStringOfNames).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder().WithDifferentlyTypedLoci().Build(),
                }
            },

            // Three field tgs donors
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                GenotypeCriteria = new GenotypeCriteriaBuilder()
                    .WithTgsTypingCategoryAtAllLoci(TgsHlaTypingCategory.ThreeFieldAllele)
                    .Build(),
            },

            // Two field tgs donors
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                GenotypeCriteria = new GenotypeCriteriaBuilder()
                    .WithTgsTypingCategoryAtAllLoci(TgsHlaTypingCategory.TwoFieldAllele)
                    .Build(),
            },

            // Allele string of subtypes
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                GenotypeCriteria = new GenotypeCriteriaBuilder().WithAlleleStringOfSubtypesPossibleAtAllLoci().Build(),
                DatabaseDonorSpecifications = new List<DatabaseDonorSpecification>
                {
                    new DatabaseDonorSelectionCriteriaBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.AlleleStringOfSubtypes).Build(),
                }
            },

            // P Group matching
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                GenotypeCriteria = new GenotypeCriteriaBuilder().WithMatchLevelPossibleAtAllLoci(MatchLevel.PGroup).Build(),
            },

            // G Group matching
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                GenotypeCriteria = new GenotypeCriteriaBuilder().WithMatchLevelPossibleAtAllLoci(MatchLevel.GGroup).Build(),
            },

            // CDna matching
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                GenotypeCriteria = new GenotypeCriteriaBuilder().WithMatchLevelPossibleAtAllLoci(MatchLevel.CDna).Build(),
            },

            // Protein matching
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                GenotypeCriteria = new GenotypeCriteriaBuilder().WithMatchLevelPossibleAtAllLoci(MatchLevel.Protein).Build(),
            },

            // Three field matching (fourth field difference)
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                GenotypeCriteria = new GenotypeCriteriaBuilder()
                    .WithMatchLevelPossibleAtAllLoci(MatchLevel.FirstThreeFieldAllele)
                    .Build(),
            },

            // Two field matching (third field difference)
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                GenotypeCriteria = new GenotypeCriteriaBuilder()
                    .WithMatchLevelPossibleAtAllLoci(MatchLevel.FirstTwoFieldAllele)
                    .Build(),
            },

            // Homozygous at A
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                GenotypeCriteria = new GenotypeCriteriaBuilder().HomozygousAtLocus(Locus.A).Build(),
                DatabaseDonorSpecifications = new List<DatabaseDonorSpecification>
                {
                    new DatabaseDonorSelectionCriteriaBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.Tgs).Build()
                }
            },

            // Cord
            new MetaDonor
            {
                DonorType = DonorType.Cord,
                GenotypeCriteria = new GenotypeCriteriaBuilder().Build(),
            },

            // Null alleles
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                GenotypeCriteria = new GenotypeCriteriaBuilder().WithNullAlleleAtAllLoci().Build(),
            },
            
            // Single null allele at A
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                GenotypeCriteria = new GenotypeCriteriaBuilder().WithNullAlleleAtPosition(Locus.A, LocusPosition.One).Build(),
            },

            // Non null alleles with an expression suffix
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                GenotypeCriteria = new GenotypeCriteriaBuilder()
                    .WithNonNullExpressionSuffixAtLocus(Locus.A)
                    .WithNonNullExpressionSuffixAtLocus(Locus.B)
                    .WithNonNullExpressionSuffixAtLocus(Locus.C)
                    .WithNonNullExpressionSuffixAtLocus(Locus.Dpb1)
                    .WithNonNullExpressionSuffixAtLocus(Locus.Dqb1)
                    .Build(),
            },

            // Cord with mismatches from the genotype 
            new MetaDonor
            {
                DonorType = DonorType.Cord,
                GenotypeCriteria = new GenotypeCriteriaBuilder().Build(),
                DatabaseDonorSpecifications = new List<DatabaseDonorSpecification>
                {
                    new DatabaseDonorSelectionCriteriaBuilder().WithNonGenotypeAlleleAtPosition(Locus.A, LocusPosition.One).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder().WithNonGenotypeAlleleAtLocus(Locus.A).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder()
                        .WithNonGenotypeAlleleAtLocus(Locus.A)
                        .WithNonGenotypeAlleleAtPosition(Locus.B, LocusPosition.One)
                        .Build(),
                    new DatabaseDonorSelectionCriteriaBuilder()
                        .WithNonGenotypeAlleleAtLocus(Locus.A)
                        .WithNonGenotypeAlleleAtLocus(Locus.B)
                        .Build(),
                    new DatabaseDonorSelectionCriteriaBuilder()
                        .WithNonGenotypeAlleleAtLocus(Locus.Dqb1)
                        .Build(),
                    new DatabaseDonorSelectionCriteriaBuilder()
                        .WithNonGenotypeAlleleAtPosition(Locus.Dqb1, LocusPosition.One)
                        .Build(),
                    // Exact match must be last, to ensure ranking tests do not give a false positive.
                    new DatabaseDonorSelectionCriteriaBuilder()
                        .Build(),
                }
            },

            // Donors with unambiguous typings, as well as ambiguous allele strings containing multiple/single p-groups - needed for confidence tests
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                GenotypeCriteria = new GenotypeCriteriaBuilder().WithStringOfSingleAndMultiplePGroupsPossibleAtAllLoci().Build(),
                DatabaseDonorSpecifications = new List<DatabaseDonorSpecification>
                {
                    new DatabaseDonorSelectionCriteriaBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.Tgs).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.Unambiguous).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder()
                        .WithAllLociAtTypingResolution(HlaTypingResolution.AlleleStringOfNamesWithMultiplePGroups).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.AlleleStringOfNamesWithSinglePGroup)
                        .Build(),
                }
            },
            
            // Donors without typing at optional Loci
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                GenotypeCriteria = new GenotypeCriteriaBuilder().Build(),
                DatabaseDonorSpecifications = new List<DatabaseDonorSpecification>
                {
                    new DatabaseDonorSelectionCriteriaBuilder().UntypedAtLocus(Locus.C).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder().UntypedAtLocus(Locus.Dqb1).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder().UntypedAtLocus(Locus.Dpb1).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder().UntypedAtLocus(Locus.C).UntypedAtLocus(Locus.Dqb1).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder().UntypedAtLocus(Locus.C).UntypedAtLocus(Locus.Dpb1).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder().UntypedAtLocus(Locus.Dpb1).UntypedAtLocus(Locus.Dqb1).Build(),
                    new DatabaseDonorSelectionCriteriaBuilder().UntypedAtLocus(Locus.C).UntypedAtLocus(Locus.Dpb1).UntypedAtLocus(Locus.Dqb1).Build(),
                }
            },
        };

        /// <summary>
        /// Used when testing a selection of four-field TGS typed meta-donors
        /// </summary>
        private static readonly IEnumerable<MetaDonor> FourFieldDonorRange = Enumerable.Range(0, SmallDonorRangeCount + 1).Select(i => new MetaDonor
        {
            DonorType = DonorType.Adult,
            GenotypeCriteria = new GenotypeCriteriaBuilder().WithTgsTypingCategoryAtAllLoci(TgsHlaTypingCategory.FourFieldAllele).Build(),
        });

        /// <summary>
        /// Used when testing a selection of three-field TGS typed meta-donors
        /// </summary>
        private static readonly IEnumerable<MetaDonor> ThreeFieldDonorRange = Enumerable.Range(0, SmallDonorRangeCount + 1).Select(i => new MetaDonor
        {
            DonorType = DonorType.Adult,
            GenotypeCriteria = new GenotypeCriteriaBuilder().WithTgsTypingCategoryAtAllLoci(TgsHlaTypingCategory.ThreeFieldAllele).Build(),
        });

        /// <summary>
        /// Used when testing a selection of two-field TGS meta-donors
        /// </summary>
        private static readonly IEnumerable<MetaDonor> TwoFieldDonorRange = Enumerable.Range(0, SmallDonorRangeCount + 1).Select(i => new MetaDonor
        {
            DonorType = DonorType.Adult,
            GenotypeCriteria = new GenotypeCriteriaBuilder().WithTgsTypingCategoryAtAllLoci(TgsHlaTypingCategory.TwoFieldAllele).Build(),
        });

        /// <summary>
        /// Used when testing a selection of TGS meta-donors with an arbitrary number of fields
        /// </summary>
        private static readonly IEnumerable<MetaDonor> ArbitraryFieldCountDonorRange = Enumerable.Range(0, SmallDonorRangeCount + 1).Select(i =>
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                GenotypeCriteria = new GenotypeCriteriaBuilder().WithTgsTypingCategoryAtAllLoci(TgsHlaTypingCategory.Arbitrary).Build(),
            });

        /// <summary>
        /// Used when testing a selection of meta-donors with an arbitrary typing resolution at all loci
        /// </summary>
        private static readonly IEnumerable<MetaDonor> ArbitraryResolutionDonorRange = Enumerable.Range(0, LargeDonorRangeCount + 1).Select(i =>
            new MetaDonor
            {
                DonorType = DonorType.Adult,
                GenotypeCriteria = new GenotypeCriteriaBuilder().WithTgsTypingCategoryAtAllLoci(TgsHlaTypingCategory.Arbitrary).Build(),
                DatabaseDonorSpecifications = new List<DatabaseDonorSpecification>
                {
                    new DatabaseDonorSelectionCriteriaBuilder().WithAllLociAtTypingResolution(HlaTypingResolution.Arbitrary).Build(),
                }
            });

        public IEnumerable<MetaDonor> MetaDonors =>
            IndividualMetaDonors
                .Concat(FourFieldDonorRange)
                .Concat(ThreeFieldDonorRange)
                .Concat(TwoFieldDonorRange)
                .Concat(ArbitraryFieldCountDonorRange)
                .Concat(ArbitraryResolutionDonorRange);
    }
}