using System.Threading.Tasks;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.HaplotypeFrequencySets;
using Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.FrequencySetFile;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.HaplotypeFrequencySets
{
    [TestFixture]
    public class HaplotypeFrequencySetTests
    {
        private const string DefaultEthnicityCode = "ethnicity-code";
        private const string DefaultRegistryCode = "registry-code";

        private static readonly IndividualPopulationData DefaultSpecificPopulation = IndividualPopulationDataBuilder.New
            .ForRegistry(DefaultRegistryCode)
            .ForEthnicity(DefaultEthnicityCode)
            .Build();

        private static readonly IndividualPopulationData DefaultRegistryOnlyPopulation = IndividualPopulationDataBuilder.New
            .ForRegistry(DefaultRegistryCode)
            .Build();

        private static readonly IndividualPopulationData GlobalPopulation = IndividualPopulationDataBuilder.New.Build();

        private readonly IHaplotypeFrequencySetService service;
        private readonly IFrequencySetService importService;

        public HaplotypeFrequencySetTests()
        {
            service = DependencyInjection.DependencyInjection.Provider.GetService<IHaplotypeFrequencySetService>();
            importService = DependencyInjection.DependencyInjection.Provider.GetService<IFrequencySetService>();
        }

        [SetUp]
        public async Task SetUp()
        {
            await ImportHaplotypeSet(DefaultSpecificPopulation);
            await ImportHaplotypeSet(DefaultRegistryOnlyPopulation);
            await ImportHaplotypeSet(GlobalPopulation);
        }

        [Test]
        public async Task GetHaplotypeSet_WhenPatientAndDonorShareSameInformation_ReturnsSharedFrequencySet()
        {
            var donorInfo = DefaultSpecificPopulation;
            var patientInfo = DefaultSpecificPopulation;

            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.DonorSet.Should().BeEquivalentTo(donorInfo);
            result.PatientSet.Should().BeEquivalentTo(patientInfo);
        }

        [Test]
        public async Task GetHaplotypeSet_WhenPatientAndDonorShareRegistryButNotEthnicity_ReturnsDifferentSets()
        {
            const string nonDefaultEthnicityCode = "non-default-ethnicity-code";
            await ImportHaplotypeSet(DefaultRegistryCode, nonDefaultEthnicityCode);

            var donorInfo = DefaultSpecificPopulation;
            var patientInfo = IndividualPopulationDataBuilder.New.ForRegistry(DefaultRegistryCode).ForEthnicity(nonDefaultEthnicityCode).Build();

            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.DonorSet.Should().BeEquivalentTo(donorInfo);
            result.PatientSet.Should().BeEquivalentTo(patientInfo);
        }

        [Test]
        public async Task GetHaplotypeSet_WhenPatientHasUnrepresentedRegistry_ReturnsDonorsRegistry()
        {
            var donorInfo = DefaultSpecificPopulation;
            var patientInfo = IndividualPopulationDataBuilder.New.ForRegistry("not-recognised").ForEthnicity(DefaultEthnicityCode).Build();

            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.PatientSet.RegistryCode.Should().Be(donorInfo.RegistryCode);
        }

        [Test]
        public async Task GetHaplotypeSet_WhenPatientHasNoRegistryData_ReturnsDonorsRegistry()
        {
            var donorInfo = DefaultSpecificPopulation;
            var patientInfo = IndividualPopulationDataBuilder.New.ForRegistry(null).ForEthnicity(DefaultEthnicityCode).Build();

            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.PatientSet.RegistryCode.Should().BeEquivalentTo(donorInfo.RegistryCode);
        }

        [Test]
        public async Task GetHaplotypeSet_WhenPatientHasDifferentRepresentedRegistryToDonor_ReturnsPatientRegistry()
        {
            const string registryCode = "new-registry";
            await ImportHaplotypeSet(registryCode, DefaultEthnicityCode);

            var donorInfo = DefaultSpecificPopulation;
            var patientInfo = IndividualPopulationDataBuilder.New.ForRegistry(registryCode).ForEthnicity(DefaultEthnicityCode).Build();

            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.PatientSet.RegistryCode.Should().BeEquivalentTo(patientInfo.RegistryCode);
        }

        [Test]
        public async Task GetHaplotypeSet_WhenDonorAndPatientHaveUnrepresentedEthnicity_ReturnsARegistrySpecificSet()
        {
            var donorInfo = IndividualPopulationDataBuilder.New.ForRegistry(DefaultRegistryCode).ForEthnicity("new-ethnicity-donor").Build();
            var patientInfo = IndividualPopulationDataBuilder.New.ForRegistry(DefaultRegistryCode).ForEthnicity("new-ethnicity-patient").Build();

            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.DonorSet.Should().BeEquivalentTo(DefaultRegistryOnlyPopulation);
            result.PatientSet.Should().BeEquivalentTo(DefaultRegistryOnlyPopulation);
        }

        [Test]
        public async Task GetHaplotypeSet_WhenNoEthnicityInformationProvided_ReturnsARegistrySpecificSet()
        {
            var donorInfo = IndividualPopulationDataBuilder.New.ForRegistry(DefaultRegistryCode).Build();
            var patientInfo = IndividualPopulationDataBuilder.New.ForRegistry(DefaultRegistryCode).Build();

            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.DonorSet.Should().BeEquivalentTo(DefaultRegistryOnlyPopulation);
            result.PatientSet.Should().BeEquivalentTo(DefaultRegistryOnlyPopulation);
        }

        [Test]
        public async Task GetHaplotypeSet_WhenNoInformationPresent_ReturnsGenericSet()
        {
            var donorInfo = IndividualPopulationDataBuilder.New.Build();
            var patientInfo = IndividualPopulationDataBuilder.New.Build();

            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.DonorSet.Should().BeEquivalentTo(GlobalPopulation);
            result.PatientSet.Should().BeEquivalentTo(GlobalPopulation);
        }

        [Test]
        public async Task GetHaplotypeSet_WhenAllInformationUnrepresented_ReturnsGenericSet()
        {
            var donorInfo = IndividualPopulationDataBuilder.New
                .ForRegistry("unrepresented-registry-donor")
                .ForEthnicity("unrepresented-ethnicity-donor")
                .Build();
            var patientInfo = IndividualPopulationDataBuilder.New
                .ForRegistry("unrepresented-registry-patient")
                .ForEthnicity("unrepresented-ethnicity-patient")
                .Build();

            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.DonorSet.Should().BeEquivalentTo(GlobalPopulation);
            result.PatientSet.Should().BeEquivalentTo(GlobalPopulation);
        }

        [Test]
        public async Task GetHaplotypeSet_WhenEthnicityIsValidButRegistryIsNot_ReturnsGenericSet()
        {
            var donorInfo = IndividualPopulationDataBuilder.New.ForRegistry("new-registry-donor").ForEthnicity(DefaultEthnicityCode).Build();
            var patientInfo = IndividualPopulationDataBuilder.New.ForRegistry("new-registry-patient").ForEthnicity(DefaultEthnicityCode).Build();

            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.DonorSet.Should().BeEquivalentTo(GlobalPopulation);
            result.PatientSet.Should().BeEquivalentTo(GlobalPopulation);
        }

        private async Task ImportHaplotypeSet(IndividualPopulationData populationData)
        {
            await ImportHaplotypeSet(populationData.RegistryCode, populationData.EthnicityCode);
        }

        private async Task ImportHaplotypeSet(string registry, string ethnicity)
        {
            using var file = FrequencySetFileBuilder.New(registry, ethnicity).Build();
            await importService.ImportFrequencySet(file);
        }
    }
}