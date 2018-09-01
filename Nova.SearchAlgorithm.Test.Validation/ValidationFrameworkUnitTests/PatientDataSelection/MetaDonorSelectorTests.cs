using System.Collections.Generic;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Builders;
using Nova.SearchAlgorithm.Test.Validation.TestData.Exceptions;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.PatientDataSelection;
using Nova.SearchAlgorithm.Test.Validation.TestData.Repositories;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection;
using NSubstitute;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationFrameworkUnitTests.PatientDataSelection
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

            var criteria = new MetaDonorSelectionCriteria
            {
                MatchingDonorType = donorType,
            };

            Assert.Throws<MetaDonorNotFoundException>(() => metaDonorSelector.GetMetaDonor(criteria));
        }

        [Test]
        public void GetMetaDonor_WhenNoMetaDonorsExistAtSpecifiedRegistry_ThrowsException()
        {
            const RegistryCode registry = RegistryCode.AN;
            const RegistryCode anotherRegistry = RegistryCode.DKMS;

            var metaDonors = new List<MetaDonor>
            {
                new MetaDonorBuilder().AtRegistry(anotherRegistry).Build(),
            };
            metaDonorRepository.AllMetaDonors().Returns(metaDonors);

            var criteria = new MetaDonorSelectionCriteria
            {
                MatchingRegistry = registry,
            };

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

            var criteria = new MetaDonorSelectionCriteria
            {
                MatchingTgsTypingCategories = new PhenotypeInfo<TgsHlaTypingCategory>(TgsHlaTypingCategory.ThreeFieldAllele),
                MatchLevels = new PhenotypeInfo<MatchLevel>(MatchLevel.GGroup),
            };

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

            var criteria = new MetaDonorSelectionCriteria
            {
                MatchingTgsTypingCategories = new PhenotypeInfo<TgsHlaTypingCategory>(TgsHlaTypingCategory.Arbitrary),
                MatchLevels = new PhenotypeInfo<MatchLevel>(),
            };

            Assert.Throws<MetaDonorNotFoundException>(() => metaDonorSelector.GetMetaDonor(criteria));
        }

        [Test]
        public void GetMetaDonor_WhenPGroupLevelMatchRequired_AndNoMetaDonorHasPGroupMatchPossible_ThrowsException()
        {
            var criteria = new MetaDonorSelectionCriteria
            {
                MatchLevels = new PhenotypeInfo<MatchLevel>(MatchLevel.PGroup),
            };

            Assert.Throws<MetaDonorNotFoundException>(() => metaDonorSelector.GetMetaDonor(criteria));
        }

        [Test]
        public void GetMetaDonor_WhenGGroupLevelMatchRequired_AndNoMetaDonorHasGGroupMatchPossible_ThrowsException()
        {
            var criteria = new MetaDonorSelectionCriteria
            {
                MatchLevels = new PhenotypeInfo<bool>().Map((l, p, noop) => MatchLevel.GGroup),
            };

            Assert.Throws<MetaDonorNotFoundException>(() => metaDonorSelector.GetMetaDonor(criteria));
        }

        [Test]
        public void GetMetaDonor_WhenThreeFieldLevelMatchRequired_AndNoMetaDonorHasThreeFieldMatchPossible_ThrowsException()
        {
            var criteria = new MetaDonorSelectionCriteria
            {
                MatchLevels = new PhenotypeInfo<MatchLevel>(MatchLevel.FirstThreeFieldAllele),
            };

            Assert.Throws<MetaDonorNotFoundException>(() => metaDonorSelector.GetMetaDonor(criteria));
        }

        [Test]
        public void GetMetaDonor_WhenTwoFieldLevelMatchRequired_AndNoMetaDonorHasThreeFieldMatchPossible_ThrowsException()
        {
            var criteria = new MetaDonorSelectionCriteria
            {
                MatchLevels = new PhenotypeInfo<MatchLevel>(MatchLevel.FirstTwoFieldAllele),
            };

            Assert.Throws<MetaDonorNotFoundException>(() => metaDonorSelector.GetMetaDonor(criteria));
        }

        [Test]
        public void GetMetaDonor_WhenNoMetaDonorsContainDonorAtExpectedResolution_ThrowsException()
        {
            var criteria = new MetaDonorSelectionCriteria
            {
                DatabaseDonorDetailsSets = new List<DatabaseDonorSpecification>
                {
                    new DatabaseDonorSpecification
                    {
                        MatchingTypingResolutions = new PhenotypeInfo<HlaTypingResolution>(HlaTypingResolution.NmdpCode)
                    }
                },
                MatchLevels = new PhenotypeInfo<MatchLevel>(),
            };

            Assert.Throws<MetaDonorNotFoundException>(() => metaDonorSelector.GetMetaDonor(criteria));
        }
        
        [Test]
        public void GetMetaDonor_WhenNoMetaDonorsContainDonorWithExpectedGenotypeMismatches_ThrowsException()
        {
            var criteria = new MetaDonorSelectionCriteria
            {
                DatabaseDonorDetailsSets = new List<DatabaseDonorSpecification>
                {
                    new DatabaseDonorSpecification
                    {
                        ShouldMatchGenotype = new PhenotypeInfo<bool>(false)
                    }
                },
                MatchLevels = new PhenotypeInfo<MatchLevel>(),
            };

            Assert.Throws<MetaDonorNotFoundException>(() => metaDonorSelector.GetMetaDonor(criteria));
        }

        [Test]
        public void GetMetaDonor_ReturnsMetaDonorAtMatchingRegistry()
        {
            const RegistryCode registryCode = RegistryCode.AN;
            const RegistryCode anotherRegistryCode = RegistryCode.DKMS;

            var metaDonors = new List<MetaDonor>
            {
                new MetaDonorBuilder().AtRegistry(anotherRegistryCode).Build(),
                new MetaDonorBuilder().AtRegistry(registryCode).Build(),
            };

            metaDonorRepository.AllMetaDonors().Returns(metaDonors);

            var criteria = new MetaDonorSelectionCriteria
            {
                MatchingRegistry = registryCode,
                MatchLevels = new PhenotypeInfo<MatchLevel>(),
            };

            var metaDonor = metaDonorSelector.GetMetaDonor(criteria);

            metaDonor.Registry.Should().Be(registryCode);
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

            var criteria = new MetaDonorSelectionCriteria
            {
                MatchingDonorType = donorType,

                MatchLevels = new PhenotypeInfo<MatchLevel>(),
            };

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

            var criteria = new MetaDonorSelectionCriteria
            {
                MatchLevels = new PhenotypeInfo<MatchLevel>(),
                IsHomozygous = new LocusInfo<bool>(false)
            };

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

            var criteria = new MetaDonorSelectionCriteria
            {
                MatchLevels = new PhenotypeInfo<MatchLevel>(),
                IsHomozygous = new LocusInfo<bool>(true)
            };

            var metaDonor = metaDonorSelector.GetMetaDonor(criteria);

            metaDonor.Should().NotBeNull();
        }

        [Test]
        public void GetMetaDonor_WhenLocusRequiredToBeHomozygous_AndMetaDonorIsNotHomozygous_ThrowsException()
        {
            var criteria = new MetaDonorSelectionCriteria
            {
                MatchLevels = new PhenotypeInfo<MatchLevel>(),
                IsHomozygous = new LocusInfo<bool>(true)
            };

            Assert.Throws<MetaDonorNotFoundException>(() => metaDonorSelector.GetMetaDonor(criteria));
        }

        [Test]
        public void GetMetaDonor_WhenAlleleStringShouldHaveDifferentAlleleGroups_AndNoMetaDonorHasDifferentAlleleGroups_ThrowsException()
        {
            var criteria = new MetaDonorSelectionCriteria
            {
                MatchLevels = new PhenotypeInfo<MatchLevel>(),
                AlleleStringContainsDifferentAntigenGroups = new PhenotypeInfo<bool>(true)
            };

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

            var criteria = new MetaDonorSelectionCriteria
            {
                MatchLevels = new PhenotypeInfo<MatchLevel>(),
                AlleleStringContainsDifferentAntigenGroups = new PhenotypeInfo<bool>(true)
            };

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

            var criteria = new MetaDonorSelectionCriteria
            {
                MatchLevels = new PhenotypeInfo<MatchLevel>(),
                MetaDonorsToSkip = metaDonors.Count,
            };

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

            var criteria = new MetaDonorSelectionCriteria
            {
                MatchLevels = new PhenotypeInfo<MatchLevel>(),
                MetaDonorsToSkip = 1,
            };

            var metaDonor = metaDonorSelector.GetMetaDonor(criteria);
            metaDonor.Should().Be(metaDonors[1]);
        }

        [Test]
        public void GetMetaDonor_WhenShouldHaveExpressionSuffix_MatchesDonorFromExpressionSuffixDataset()
        {
            var metaDonors = new List<MetaDonor>
            {
                new MetaDonorBuilder()
                    .WithGenotypeCriteria(new GenotypeCriteriaBuilder().WithNonNullExpressionSuffixAtLocus(Locus.A).Build())
                    .Build(),
            };

            metaDonorRepository.AllMetaDonors().Returns(metaDonors);

            var criteria = new MetaDonorSelectionCriteria
            {
                MatchLevels = new PhenotypeInfo<MatchLevel>(),
                HasNonNullExpressionSuffix = new PhenotypeInfo<bool>().Map((l, p, noop) => l == Locus.A),
            };

            var metaDonor = metaDonorSelector.GetMetaDonor(criteria);
            metaDonor.Should().NotBeNull();
        }

        [Test]
        public void GetMetaDonor_WhenShouldHaveExpressionSuffix_AndNoDonorHasExpressionSuffix_ThrowsException()
        {
            var criteria = new MetaDonorSelectionCriteria
            {
                MatchLevels = new PhenotypeInfo<MatchLevel>(),
                HasNonNullExpressionSuffix = new PhenotypeInfo<bool>(true),
            };

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

            var criteria = new MetaDonorSelectionCriteria
            {
                MatchLevels = new PhenotypeInfo<MatchLevel>(),
                HasNonNullExpressionSuffix = new PhenotypeInfo<bool>(false),
            };

            Assert.Throws<MetaDonorNotFoundException>(() => metaDonorSelector.GetMetaDonor(criteria));
        }
    }
}