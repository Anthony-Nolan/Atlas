using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
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
    public class PatientDataSelectorTests
    {
        private List<MetaDonor> metaDonors;
        private IMetaDonorRepository metaDonorRepository;
        private IMetaDonorSelector metaDonorSelector;

        [SetUp]
        public void SetUp()
        {
            metaDonorRepository = Substitute.For<IMetaDonorRepository>();

            metaDonorSelector = new MetaDonorSelector(metaDonorRepository);
        }

        [Test]
        public void GetMetaDonor_ReturnsMetaDonorAtMatchingRegistry()
        {
            const RegistryCode registryCode = RegistryCode.AN;
            const RegistryCode anotherRegistryCode = RegistryCode.DKMS;

            var matchingTgsTypingCategories = new PhenotypeInfo<TgsHlaTypingCategory>();
            metaDonors = new List<MetaDonor>
            {
                new MetaDonor
                {
                    GenotypeCriteria = new GenotypeCriteria {TgsHlaCategories = matchingTgsTypingCategories},
                    Registry = anotherRegistryCode,
                },
                new MetaDonor
                {
                    GenotypeCriteria = new GenotypeCriteria {TgsHlaCategories = matchingTgsTypingCategories},
                    Registry = registryCode,
                }
            };

            metaDonorRepository.AllMetaDonors().Returns(metaDonors);

            var criteria = new MetaDonorSelectionCriteria
            {
                MatchingRegistry = registryCode,
                MatchingTgsTypingCategories = matchingTgsTypingCategories,
                MatchLevels = new PhenotypeInfo<MatchLevel>(),
            };

            var metaDonor = metaDonorSelector.GetMetaDonor(criteria);

            metaDonor.Registry.Should().Be(registryCode);
        }

        [Test]
        public void GetMetaDonor_ReturnsHlaForDonorOfMatchingType()
        {
            const DonorType donorType = DonorType.Adult;
            const DonorType anotherDonorType = DonorType.Cord;

            var matchingTgsTypingCategories = new PhenotypeInfo<TgsHlaTypingCategory>();
            metaDonors = new List<MetaDonor>
            {
                new MetaDonor
                {
                    GenotypeCriteria = new GenotypeCriteria {TgsHlaCategories = matchingTgsTypingCategories},
                    DonorType = anotherDonorType,
                },
                new MetaDonor
                {
                    GenotypeCriteria = new GenotypeCriteria {TgsHlaCategories = matchingTgsTypingCategories},
                    DonorType = donorType,
                }
            };

            metaDonorRepository.AllMetaDonors().Returns(metaDonors);

            var criteria = new MetaDonorSelectionCriteria
            {
                MatchingDonorType = donorType,
                MatchingTgsTypingCategories = matchingTgsTypingCategories,
                MatchLevels = new PhenotypeInfo<MatchLevel>(),
            };

            var metaDonor = metaDonorSelector.GetMetaDonor(criteria);

            metaDonor.DonorType.Should().Be(donorType);
        }
    }
}