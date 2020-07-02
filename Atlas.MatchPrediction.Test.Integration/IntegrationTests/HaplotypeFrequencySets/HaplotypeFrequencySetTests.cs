using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.HaplotypeFrequencySets;
using Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.FrequencySetFile;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute.ExceptionExtensions;
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
                // Note: Generic Population data is not added here to allow us to check error state 
                // This is instead added in the relevant test.
                new IndividualPopulationData { EthnicityId = "01", RegistryId = "12" },
                new IndividualPopulationData { EthnicityId = "02", RegistryId = "12" },
                new IndividualPopulationData { EthnicityId = "03", RegistryId = "22" },
                new IndividualPopulationData { RegistryId = "12" },
                new IndividualPopulationData()
            };
            
            await ImportAllHaplotypeSets(data);
        }

        [Test]
        public async Task GetHaplotypeSet_GetsTheCorrectSet_WhenPatientAndDonorShareSameInformation()
        {
            var donorInfo = new IndividualPopulationData {EthnicityId = "01", RegistryId = "12"};
            var patientInfo = new IndividualPopulationData {EthnicityId = "01", RegistryId = "12"};

            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.DonorSet.EthnicityCode.Should().Be("01");
            result.DonorSet.RegistryCode.Should().Be("12");
            result.PatientSet.EthnicityCode.Should().Be("01");
            result.PatientSet.RegistryCode.Should().Be("12");
        }
        
        [Test]
        public async Task GetHaplotypeSet_GetsTheCorrectSet_WhenPatientHasNoRegistryData()
        {
            var donorInfo = new IndividualPopulationData {EthnicityId = "01", RegistryId = "12"};
            var patientInfo = new IndividualPopulationData {EthnicityId = "01"};

            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.DonorSet.EthnicityCode.Should().Be("01");
            result.DonorSet.RegistryCode.Should().Be("12");
            result.PatientSet.EthnicityCode.Should().Be("01");
            result.PatientSet.RegistryCode.Should().Be("12");
        }

        [Test]
        public async Task GetHaplotyopeSet_GetsTheCorrectSet_WhenDonorAndPatientShareRegistryButNotEthnicity()
        {
            var donorInfo = new IndividualPopulationData {EthnicityId = "NON_EXISTENT", RegistryId = "12"};
            var patientInfo = new IndividualPopulationData {EthnicityId = "NON_EXISTENT_2", RegistryId = "12"};
            
            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.DonorSet.RegistryCode.Should().Be("12");
            result.DonorSet.EthnicityCode.Should().BeNull();
            result.PatientSet.RegistryCode.Should().Be("12");
            result.PatientSet.EthnicityCode.Should().BeNull();
        }

        [Test]
        public async Task GetHaplotypeSet_GetsTheCorrectSet_WhenNoEthnicityInformationProvided()
        {
            var donorInfo = new IndividualPopulationData { RegistryId = "12"};
            var patientInfo = new IndividualPopulationData { RegistryId = "12"};
            
            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.DonorSet.RegistryCode.Should().Be("12");
            result.DonorSet.EthnicityCode.Should().BeNull();
            result.PatientSet.RegistryCode.Should().Be("12");
            result.PatientSet.EthnicityCode.Should().BeNull();
        }

        [Test]
        public async Task GetHaplotypeSet_GetsGenericSet_WhenNoInformationPresent()
        {
            var donorInfo = new IndividualPopulationData();
            var patientInfo = new IndividualPopulationData();

            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.DonorSet.EthnicityCode.Should().BeNull();
            result.DonorSet.RegistryCode.Should().BeNull();
            result.PatientSet.EthnicityCode.Should().BeNull();
            result.PatientSet.RegistryCode.Should().BeNull();

        }
        
        private async Task ImportAllHaplotypeSets(IEnumerable<IndividualPopulationData> data)
        {
            var tasks = data.Select(set => ImportHaplotypeSet(set.RegistryId, set.EthnicityId));
            await Task.WhenAll(tasks);
        }
        
        private async Task ImportHaplotypeSet(string registry, string ethnicity)
        {
            using var file = FrequencySetFileBuilder.New(registry, ethnicity).Build();
            await importService.ImportFrequencySet(file);
        }
    }
}