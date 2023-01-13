using System.Collections.Generic;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Exceptions;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.PatientDataSelection;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Services.PatientDataSelection.DataSelectors;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Validation.ValidationFrameworkUnitTests.PatientDataSelection.DataSelectors
{
    public class DatabaseDonorSelectorTests
    {
        private IDatabaseDonorSelector donorSelector;

        [SetUp]
        public void SetUp()
        {
            donorSelector = new DatabaseDonorSelector();
            ;
        }

        [Test]
        public void GetExpectedMatchingDonorId_ReturnsDonorId()
        {
            const int donorId = 1;
            var metaDonor = new MetaDonor
            {
                DatabaseDonors = new List<Donor>
                {
                    new Donor
                    {
                        DonorId = donorId
                    }
                }
            };
            var criteria = new DatabaseDonorSpecification();

            var id = donorSelector.GetExpectedMatchingDonorId(metaDonor, criteria);

            id.Should().Be(donorId);
        }

        [Test]
        public void GetExpectedMatchingDonorId_WhenMultipleDonorsExistForMetaDonor_ReturnsDonorIdOfMatchingDonor()
        {
            const int donorIdMatching = 1;
            const int donorIdNotMatching = 2;
            var matchingTypingResolutions = new PhenotypeInfo<HlaTypingResolution>(HlaTypingResolution.XxCode);

            var metaDonor = new MetaDonor
            {
                DatabaseDonors = new List<Donor>
                {
                    new Donor
                    {
                        DonorId = donorIdNotMatching,
                    },
                    new Donor
                    {
                        DonorId = donorIdMatching
                    }
                },
                DatabaseDonorSpecifications = new List<DatabaseDonorSpecification>
                {
                    new DatabaseDonorSpecification(),
                    new DatabaseDonorSpecification
                    {
                        MatchingTypingResolutions = matchingTypingResolutions,
                    }
                }
            };
            var criteria = new DatabaseDonorSpecification
            {
                MatchingTypingResolutions = matchingTypingResolutions
            };

            var id = donorSelector.GetExpectedMatchingDonorId(metaDonor, criteria);

            id.Should().Be(donorIdMatching);
        }

        [Test]
        public void GetExpectedMatchingDonorId_WhenNoDonorExistsWithMatchingTypingResolution_ThrowsException()
        {
            const int donorId = 1;
            var metaDonor = new MetaDonor
            {
                DatabaseDonors = new List<Donor>
                {
                    new Donor
                    {
                        DonorId = donorId
                    }
                }
            };
            var criteria = new DatabaseDonorSpecification
            {
                MatchingTypingResolutions = new PhenotypeInfo<HlaTypingResolution>
                (
                    valueA: new LocusInfo<HlaTypingResolution>(HlaTypingResolution.Serology, default)
                )
            };

            Assert.Throws<DonorNotFoundException>(() => donorSelector.GetExpectedMatchingDonorId(metaDonor, criteria));
        }
    }
}