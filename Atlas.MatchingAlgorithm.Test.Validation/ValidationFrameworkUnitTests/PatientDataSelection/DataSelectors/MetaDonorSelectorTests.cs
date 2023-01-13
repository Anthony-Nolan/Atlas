using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Builders;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Builders.Criteria;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Exceptions;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.PatientDataSelection;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Repositories;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Services.PatientDataSelection.DataSelectors;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Validation.ValidationFrameworkUnitTests.PatientDataSelection.DataSelectors
{
    [TestFixture]
    public class MetaDonorSelectorTests
    {
        private IMetaDonorRepository metaDonorRepository;
        private IMetaDonorSelector metaDonorSelector;

        [SetUp]
        public void SetUp()
        {
            metaDonorRepository = Substitute.For<IMetaDonorRepository>();

            var defaultMetaDonors = new List<MetaDonor> {new MetaDonorBuilder().Build()};
            metaDonorRepository.AllMetaDonors().Returns(defaultMetaDonors);
            
            metaDonorSelector = new MetaDonorSelector(metaDonorRepository);
        }

        [Test]
        public void GetMetaDonor_WhenNoMetaDonorsExistOfSpecifiedType_ThrowsException()
        {
            const DonorType donorType = DonorType.Adult;
            const DonorType anotherDonorType = DonorType.Cord;

            var metaDonors = new List<MetaDonor>
            {
                new MetaDonorBuilder().WithDonorType(anotherDonorType).Build(),
            };
            metaDonorRepository.AllMetaDonors().Returns(metaDonors);

            var criteria = new MetaDonorSelectionCriteriaBuilder().WithMatchingDonorType(donorType).Build();

            Assert.Throws<MetaDonorNotFoundException>(() => metaDonorSelector.GetMetaDonor(criteria));
        }

        [Test]
        public void GetMetaDonor_WhenNoMetaDonorsExistAtSpecifiedTgsResolution_ThrowsException()
        {
            var metaDonors = new List<MetaDonor>
            {
                new MetaDonorBuilder().WithGenotypeCriteria(new GenotypeCriteriaBuilder().Build()).Build()
            };
            metaDonorRepository.AllMetaDonors().Returns(metaDonors);

            var criteria = new MetaDonorSelectionCriteriaBuilder()
                .WithTgsTypingCategoryAtAllPositions(TgsHlaTypingCategory.ThreeFieldAllele)
                .WithMatchLevelAtAllPositions(MatchLevel.GGroup)
                .Build();

            Assert.Throws<MetaDonorNotFoundException>(() => metaDonorSelector.GetMetaDonor(criteria));
        }

        /// <summary>
        /// We want to guarantee that 'arbitrary' typing resolution does not match other, more specific resolutions
        ///
        /// i.e. one interpretation of a patient selector specifying 'arbitrary' data would be that it could sucessfully match any other resolution
        /// This test makes it explicit to maintainers that 'arbitrary' resolution in the patient selection should only match
        /// meta-donors that also specifiy 'arbitrary' resolution
        /// </summary>
        [Test]
        public void GetMetaDonor_WhenNoMetaDonorsExistAtSpecifiedTgsResolution_AndCriteriaAllowsArbitraryResolution_ThrowsException()
        {
            var metaDonors = new List<MetaDonor>
            {
                new MetaDonorBuilder().WithGenotypeCriteria(
                    new GenotypeCriteriaBuilder()
                        .WithTgsTypingCategoryAtAllLoci(TgsHlaTypingCategory.ThreeFieldAllele)
                        .Build()
                ).Build(),
            };
            metaDonorRepository.AllMetaDonors().Returns(metaDonors);

            var criteria = new MetaDonorSelectionCriteriaBuilder().WithTgsTypingCategoryAtAllPositions(TgsHlaTypingCategory.Arbitrary).Build();

            Assert.Throws<MetaDonorNotFoundException>(() => metaDonorSelector.GetMetaDonor(criteria));
        }

        [Test]
        public void GetMetaDonor_WhenPGroupLevelMatchRequired_AndNoMetaDonorHasPGroupMatchPossible_ThrowsException()
        {
            var criteria = new MetaDonorSelectionCriteriaBuilder().WithMatchLevelAtAllPositions(MatchLevel.PGroup).Build();

            Assert.Throws<MetaDonorNotFoundException>(() => metaDonorSelector.GetMetaDonor(criteria));
        }

        [Test]
        public void GetMetaDonor_WhenGGroupLevelMatchRequired_AndNoMetaDonorHasGGroupMatchPossible_ThrowsException()
        {
            var criteria = new MetaDonorSelectionCriteriaBuilder().WithMatchLevelAtAllPositions(MatchLevel.GGroup).Build();

            Assert.Throws<MetaDonorNotFoundException>(() => metaDonorSelector.GetMetaDonor(criteria));
        }

        [Test]
        public void GetMetaDonor_WhenThreeFieldLevelMatchRequired_AndNoMetaDonorHasThreeFieldMatchPossible_ThrowsException()
        {
            var criteria = new MetaDonorSelectionCriteriaBuilder().WithMatchLevelAtAllPositions(MatchLevel.FirstThreeFieldAllele).Build();

            Assert.Throws<MetaDonorNotFoundException>(() => metaDonorSelector.GetMetaDonor(criteria));
        }

        [Test]
        public void GetMetaDonor_WhenTwoFieldLevelMatchRequired_AndNoMetaDonorHasThreeFieldMatchPossible_ThrowsException()
        {
            var criteria = new MetaDonorSelectionCriteriaBuilder().WithMatchLevelAtAllPositions(MatchLevel.FirstTwoFieldAllele).Build();

            Assert.Throws<MetaDonorNotFoundException>(() => metaDonorSelector.GetMetaDonor(criteria));
        }

        [Test]
        public void GetMetaDonor_WhenNoMetaDonorsContainDonorAtExpectedResolution_ThrowsException()
        {
            var databaseDonorDetailsSets = new List<DatabaseDonorSpecification>
            {
                new DatabaseDonorSpecification
                {
                    MatchingTypingResolutions = new PhenotypeInfo<HlaTypingResolution>(HlaTypingResolution.NmdpCode)
                }
            };

            var criteria = new MetaDonorSelectionCriteriaBuilder().WithDatabaseDonorSpecifications(databaseDonorDetailsSets).Build();

            Assert.Throws<MetaDonorNotFoundException>(() => metaDonorSelector.GetMetaDonor(criteria));
        }
        
        [Test]
        public void GetMetaDonor_WhenNoMetaDonorsContainDonorWithExpectedGenotypeMismatches_ThrowsException()
        {
            var databaseDonorDetailsSets = new List<DatabaseDonorSpecification>
            {
                new DatabaseDonorSpecification
                {
                    ShouldMatchGenotype = new PhenotypeInfo<bool>(false)
                }
            };
            var criteria = new MetaDonorSelectionCriteriaBuilder().WithDatabaseDonorSpecifications(databaseDonorDetailsSets).Build();

            Assert.Throws<MetaDonorNotFoundException>(() => metaDonorSelector.GetMetaDonor(criteria));
        }

        [Test]
        public void GetMetaDonor_ReturnsMetaDonorOfMatchingType()
        {
            const DonorType donorType = DonorType.Adult;
            const DonorType anotherDonorType = DonorType.Cord;

            var metaDonors = new List<MetaDonor>
            {
                new MetaDonorBuilder().WithDonorType(anotherDonorType).Build(),
                new MetaDonorBuilder().WithDonorType(donorType).Build(),
            };

            metaDonorRepository.AllMetaDonors().Returns(metaDonors);

            var criteria = new MetaDonorSelectionCriteriaBuilder().WithMatchingDonorType(donorType).Build();

            var metaDonor = metaDonorSelector.GetMetaDonor(criteria);

            metaDonor.DonorType.Should().Be(donorType);
        }

        [Test]
        public void GetMetaDonor_WhenNoLocusRequiredToBeHomozygous_AndMetaDonorIsHomozygous_ReturnsMetaDonor()
        {
            var metaDonors = new List<MetaDonor>
            {
                new MetaDonorBuilder()
                    .WithGenotypeCriteria(new GenotypeCriteriaBuilder().HomozygousAtAllLoci().Build())
                    .Build(),
            };

            metaDonorRepository.AllMetaDonors().Returns(metaDonors);

            var criteria = new MetaDonorSelectionCriteriaBuilder().Build();

            var metaDonor = metaDonorSelector.GetMetaDonor(criteria);

            metaDonor.Should().NotBeNull();
        }

        [Test]
        public void GetMetaDonor_WhenLocusRequiredToBeHomozygous_AndMetaDonorIsHomozygous_ReturnsMetaDonor()
        {
            var metaDonors = new List<MetaDonor>
            {
                new MetaDonorBuilder()
                    .WithGenotypeCriteria(new GenotypeCriteriaBuilder().HomozygousAtAllLoci().Build())
                    .Build(),
            };

            metaDonorRepository.AllMetaDonors().Returns(metaDonors);

            var criteria = new MetaDonorSelectionCriteriaBuilder().HomozygousAtAllLoci().Build();

            var metaDonor = metaDonorSelector.GetMetaDonor(criteria);

            metaDonor.Should().NotBeNull();
        }

        [Test]
        public void GetMetaDonor_WhenLocusRequiredToBeHomozygous_AndMetaDonorIsNotHomozygous_ThrowsException()
        {
            var criteria = new MetaDonorSelectionCriteriaBuilder().HomozygousAtAllLoci().Build();

            Assert.Throws<MetaDonorNotFoundException>(() => metaDonorSelector.GetMetaDonor(criteria));
        }

        [Test]
        public void GetMetaDonor_WhenAlleleStringShouldHaveDifferentAlleleGroups_AndNoMetaDonorHasDifferentAlleleGroups_ThrowsException()
        {
            var criteria = new MetaDonorSelectionCriteriaBuilder().ShouldContainDifferentAntigenGroupsAtAllPositions().Build();

            Assert.Throws<MetaDonorNotFoundException>(() => metaDonorSelector.GetMetaDonor(criteria));
        }

        [Test]
        public void GetMetaDonor_WhenAlleleStringShouldHaveDifferentAlleleGroups_AndMetaDonorHasDifferentAlleleGroups_ReturnsMetaDonor()
        {
            var metaDonors = new List<MetaDonor>
            {
                new MetaDonorBuilder()
                    .WithGenotypeCriteria(new GenotypeCriteriaBuilder().WithAlleleStringContainingDifferentGroupsAtAllLoci().Build())
                    .Build(),
            };

            metaDonorRepository.AllMetaDonors().Returns(metaDonors);

            var criteria = new MetaDonorSelectionCriteriaBuilder().ShouldContainDifferentAntigenGroupsAtAllPositions().Build();

            var metaDonor = metaDonorSelector.GetMetaDonor(criteria);

            metaDonor.Should().NotBeNull();
        }

        [Test]
        public void GetMetaDonor_WhenNoMoreMetaDonorsMatch_ThrowsException()
        {
            var metaDonors = new List<MetaDonor>
            {
                new MetaDonorBuilder().Build(),
            };

            metaDonorRepository.AllMetaDonors().Returns(metaDonors);

            var criteria = new MetaDonorSelectionCriteriaBuilder().WithNumberOfDonorsToSkip(metaDonors.Count).Build();

            Assert.Throws<MetaDonorNotFoundException>(() => metaDonorSelector.GetMetaDonor(criteria));
        }

        [Test]
        public void GetMetaDonor_SkipsSpecifiedNumberOfMetaDonors()
        {
            var metaDonors = new List<MetaDonor>
            {
                new MetaDonorBuilder().Build(),
                new MetaDonorBuilder().Build(),
            };

            metaDonorRepository.AllMetaDonors().Returns(metaDonors);

            var criteria = new MetaDonorSelectionCriteriaBuilder().WithNumberOfDonorsToSkip(1).Build();

            var metaDonor = metaDonorSelector.GetMetaDonor(criteria);
            metaDonor.Should().Be(metaDonors[1]);
        }

        [Test]
        public void GetMetaDonor_WhenShouldHaveExpressionSuffix_MatchesDonorFromExpressionSuffixDataset()
        {
            const Locus locus = Locus.A;
            var metaDonors = new List<MetaDonor>
            {
                new MetaDonorBuilder()
                    .WithGenotypeCriteria(new GenotypeCriteriaBuilder().WithNonNullExpressionSuffixAtLocus(locus).Build())
                    .Build(),
            };

            metaDonorRepository.AllMetaDonors().Returns(metaDonors);

            var criteria = new MetaDonorSelectionCriteriaBuilder().WithNonNullExpressionSuffixAt(locus).Build();

            var metaDonor = metaDonorSelector.GetMetaDonor(criteria);
            metaDonor.Should().NotBeNull();
        }

        [Test]
        public void GetMetaDonor_WhenShouldHaveExpressionSuffix_AndNoDonorHasExpressionSuffix_ThrowsException()
        {
            var criteria = new MetaDonorSelectionCriteriaBuilder().WithNonNullExpressionSuffixAtAllLoci().Build();

            Assert.Throws<MetaDonorNotFoundException>(() => metaDonorSelector.GetMetaDonor(criteria));
        }

        [Test]
        public void GetMetaDonor_WhenShouldNotHaveExpressionSuffix_AndAllDonorHasExpressionSuffix_ThrowsException()
        {
            var metaDonors = new List<MetaDonor>
            {
                new MetaDonorBuilder()
                    .WithGenotypeCriteria(new GenotypeCriteriaBuilder().WithNonNullExpressionSuffixAtLocus(Locus.A).Build())
                    .Build(),
            };

            metaDonorRepository.AllMetaDonors().Returns(metaDonors);

            var criteria = new MetaDonorSelectionCriteriaBuilder().Build();

            Assert.Throws<MetaDonorNotFoundException>(() => metaDonorSelector.GetMetaDonor(criteria));
        }
        
        [Test]
        public void GetMetaDonor_WhenShouldHaveNullAlleleAtAllLoci_AndNoDonorHasNullAllele_ThrowsException()
        {
            var metaDonors = new List<MetaDonor>()
            {
                new MetaDonorBuilder().Build(),
            };
            
            metaDonorRepository.AllMetaDonors().Returns(metaDonors);
            
            var criteria = new MetaDonorSelectionCriteriaBuilder().WithNullAlleleAtAllPositions().Build();

            Assert.Throws<MetaDonorNotFoundException>(() => metaDonorSelector.GetMetaDonor(criteria));
        }
        
        [Test]
        public void GetMetaDonor_WhenShouldHaveNullAlleleAtAllLoci_AndDonorHasNullAllelesAtAllLoci_ReturnsMetaDonor()
        {
            var metaDonors = new List<MetaDonor>()
            {
                new MetaDonorBuilder()
                    .WithGenotypeCriteria(new GenotypeCriteriaBuilder().WithNullAlleleAtAllLoci().Build())
                    .Build(),
            };
            
            metaDonorRepository.AllMetaDonors().Returns(metaDonors);
            
            var criteria = new MetaDonorSelectionCriteriaBuilder().WithNullAlleleAtAllPositions().Build();

            var metaDonor = metaDonorSelector.GetMetaDonor(criteria);
            metaDonor.Should().NotBeNull();
        }
        
        [Test]
        public void GetMetaDonor_WhenShouldHaveNullAlleleAtOnePosition_AndDonorHasNullAlleleAtAllLoci_ThrowsException()
        {
            var metaDonors = new List<MetaDonor>()
            {
                new MetaDonorBuilder()
                    .WithGenotypeCriteria(new GenotypeCriteriaBuilder().WithNullAlleleAtAllLoci().Build())
                    .Build(),
            };
            
            metaDonorRepository.AllMetaDonors().Returns(metaDonors);
            
            var criteria = new MetaDonorSelectionCriteriaBuilder().WithNullAlleleAtPosition(Locus.A, LocusPosition.One).Build();

            Assert.Throws<MetaDonorNotFoundException>(() => metaDonorSelector.GetMetaDonor(criteria));
        }
        
        [Test]
        public void GetMetaDonor_WhenShouldHaveNullAlleleAtOnePosition_AndDonorHasNullAlleleAtCorrectPosition_ReturnsMetaDonor()
        {
            const Locus locus = Locus.A;
            const LocusPosition position = LocusPosition.One;
            var metaDonors = new List<MetaDonor>()
            {
                new MetaDonorBuilder()
                    .WithGenotypeCriteria(new GenotypeCriteriaBuilder().WithNullAlleleAtPosition(locus, position).Build())
                    .Build(),
            };
            
            metaDonorRepository.AllMetaDonors().Returns(metaDonors);
            
            var criteria = new MetaDonorSelectionCriteriaBuilder().WithNullAlleleAtPosition(locus, position).Build();

            var metaDonor = metaDonorSelector.GetMetaDonor(criteria);
            metaDonor.Should().NotBeNull();
        }
    }
}