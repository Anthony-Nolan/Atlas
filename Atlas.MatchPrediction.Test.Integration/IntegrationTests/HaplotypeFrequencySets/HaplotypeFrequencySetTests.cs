using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.HaplotypeFrequencySets;
using Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.FrequencySetFile;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.HaplotypeFrequencySets
{
    [TestFixture]
    public class HaplotypeFrequencySetTests
    {
        private IHaplotypeFrequencySetService service;
        private IFrequencySetService importService;

        public HaplotypeFrequencySetTests()
        {
            service = DependencyInjection.DependencyInjection.Provider.GetService<IHaplotypeFrequencySetService>();
            importService = DependencyInjection.DependencyInjection.Provider.GetService<IFrequencySetService>();
        }

        [SetUp]
        public async Task SetUpData()
        {
            var data = new List<IndividualPopulationData>
            {
                new IndividualPopulationData { EthnicityCode = "01", RegistryCode = "12" },
                new IndividualPopulationData { EthnicityCode = "02", RegistryCode = "12" },
                new IndividualPopulationData { EthnicityCode = "03", RegistryCode = "22" },
                new IndividualPopulationData { RegistryCode = "12" },
                new IndividualPopulationData()
            };
            
            await ImportAllHaplotypeSets(data);
        }

        [Test]
        public async Task GetHaplotypeSet_UsesSharedFrequencySet_WhenPatientAndDonorShareSameInformation()
        {
            var donorInfo = new IndividualPopulationData {EthnicityCode = "01", RegistryCode = "12"};
            var patientInfo = new IndividualPopulationData {EthnicityCode = "01", RegistryCode = "12"};

            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.DonorSet.Should().BeEquivalentTo(donorInfo);
            result.PatientSet.Should().BeEquivalentTo(patientInfo);
        }

        [Test]
        public async Task GetHaplotypeSet_UsesSpecificSets_WhenPatientAndDonorShareRegistryButNotEthnicity()
        {
            var donorInfo = new IndividualPopulationData {EthnicityCode = "01", RegistryCode = "12"};
            var patientInfo = new IndividualPopulationData {EthnicityCode = "02", RegistryCode = "12"};

            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.DonorSet.Should().BeEquivalentTo(donorInfo);
            result.PatientSet.Should().BeEquivalentTo(patientInfo);
        }
        
        [Test]
        public async Task GetHaplotypeSet_UsesDonorsRegistry_WhenPatientHasDifferentRegistryData()
        {
            var donorInfo = new IndividualPopulationData {EthnicityCode = "01", RegistryCode = "12"};
            var patientInfo = new IndividualPopulationData {EthnicityCode = "01", RegistryCode = "SOMETHING_ELSE"};

            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.DonorSet.Should().BeEquivalentTo(donorInfo);
            result.PatientSet.Should().BeEquivalentTo(donorInfo);
        }
        
        [Test]
        public async Task GetHaplotypeSet_UsesDonorsRegistry_WhenPatientHasNoRegistryData()
        {
            var donorInfo = new IndividualPopulationData {EthnicityCode = "01", RegistryCode = "12"};
            var patientInfo = new IndividualPopulationData {EthnicityCode = "01"};

            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.DonorSet.Should().BeEquivalentTo(donorInfo);
            result.PatientSet.Should().BeEquivalentTo(donorInfo);
        }

        [Test]
        public async Task GetHaplotypeSet_UsesARegistrySpecificSet_WhenDonorAndPatientShareRegistryButNotEthnicity()
        {
            var donorInfo = new IndividualPopulationData {EthnicityCode = "NON_EXISTENT", RegistryCode = "12"};
            var patientInfo = new IndividualPopulationData {EthnicityCode = "NON_EXISTENT_2", RegistryCode = "12"};
            
            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            var expectedSet = new IndividualPopulationData {RegistryCode = "12"};
            
            result.DonorSet.Should().BeEquivalentTo(expectedSet);
            result.PatientSet.Should().BeEquivalentTo(expectedSet);
        }

        [Test]
        public async Task GetHaplotypeSet_UsesARegistrySpecificSet_WhenNoEthnicityInformationProvided()
        {
            var donorInfo = new IndividualPopulationData { RegistryCode = "12"};
            var patientInfo = new IndividualPopulationData { RegistryCode = "12"};
            
            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            var expectedSet = new IndividualPopulationData {RegistryCode = "12"};
            
            result.DonorSet.Should().BeEquivalentTo(expectedSet);
            result.PatientSet.Should().BeEquivalentTo(expectedSet);
        }

        [Test]
        public async Task GetHaplotypeSet_GetsGenericSet_WhenNoInformationPresent()
        {
            var donorInfo = new IndividualPopulationData();
            var patientInfo = new IndividualPopulationData();

            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.DonorSet.Should().BeEquivalentTo(new IndividualPopulationData());
            result.PatientSet.Should().BeEquivalentTo(new IndividualPopulationData());
        }

        [Test]
        public async Task GetHaplotypeSet_GetsGenericSet_WhenInformationIsNotValid()
        {
            var donorInfo = new IndividualPopulationData {EthnicityCode = "NOT_VALID", RegistryCode = "NOT_VALID_2" };
            var patientInfo = new IndividualPopulationData { EthnicityCode = "NOT_VALID_2", RegistryCode = "NOT_VALID_3" };
            
            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.DonorSet.Should().BeEquivalentTo(new IndividualPopulationData());
            result.PatientSet.Should().BeEquivalentTo(new IndividualPopulationData());
        }
        
        [Test]
        public async Task GetHaplotypeSet_GetsGenericSet_WhenEthnicityIsValidButRegistryIsNot()
        {
            var donorInfo = new IndividualPopulationData {EthnicityCode = "01", RegistryCode = "NOT_VALID" };
            var patientInfo = new IndividualPopulationData { EthnicityCode = "02", RegistryCode = "NOT_VALID" };
            
            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.DonorSet.Should().BeEquivalentTo(new IndividualPopulationData());
            result.PatientSet.Should().BeEquivalentTo(new IndividualPopulationData());
        }
        
        private async Task ImportAllHaplotypeSets(IEnumerable<IndividualPopulationData> data)
        {
            var tasks = data.Select(set => ImportHaplotypeSet(set.RegistryCode, set.EthnicityCode));
            await Task.WhenAll(tasks);
        }
        
        private async Task ImportHaplotypeSet(string registry, string ethnicity)
        {
            using var file = FrequencySetFileBuilder.New(registry, ethnicity).Build();
            await importService.ImportFrequencySet(file);
        }
    }
}