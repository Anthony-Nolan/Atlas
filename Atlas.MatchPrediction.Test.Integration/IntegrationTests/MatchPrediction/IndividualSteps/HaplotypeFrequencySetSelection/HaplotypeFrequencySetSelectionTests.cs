using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.FrequencySetFile;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.IndividualSteps.HaplotypeFrequencySetSelection
{
    [TestFixture]
    public class HaplotypeFrequencySetSelectionTests
    {
        private const string DefaultEthnicityCode = "ethnicity-code";
        private const string DefaultRegistryCode = "registry-code";

        private static readonly FrequencySetMetadata DefaultSpecificPopulation = FrequencySetMetadataBuilder.New
            .ForRegistry(DefaultRegistryCode)
            .ForEthnicity(DefaultEthnicityCode)
            .Build();

        private static readonly FrequencySetMetadata DefaultRegistryOnlyPopulation = FrequencySetMetadataBuilder.New
            .ForRegistry(DefaultRegistryCode)
            .Build();

        private static readonly FrequencySetMetadata GlobalPopulation = FrequencySetMetadataBuilder.New.Build();

        private readonly IHaplotypeFrequencyService service;

        public HaplotypeFrequencySetSelectionTests()
        {
            service = DependencyInjection.DependencyInjection.Provider.GetService<IHaplotypeFrequencyService>();
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

            result.DonorSet.ShouldHaveEquivalentMetadata(donorInfo);
            result.PatientSet.ShouldHaveEquivalentMetadata(patientInfo);
        }

        [Test]
        public async Task GetHaplotypeSet_WhenPatientAndDonorShareRegistryButNotEthnicity_ReturnsDifferentSets()
        {
            const string nonDefaultEthnicityCode = "non-default-ethnicity-code";
            await ImportHaplotypeSet(DefaultRegistryCode, nonDefaultEthnicityCode);

            var donorInfo = DefaultSpecificPopulation;
            var patientInfo = FrequencySetMetadataBuilder.New.ForRegistry(DefaultRegistryCode).ForEthnicity(nonDefaultEthnicityCode).Build();

            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.DonorSet.ShouldHaveEquivalentMetadata(donorInfo);
            result.PatientSet.ShouldHaveEquivalentMetadata(patientInfo);
        }

        [Test]
        public async Task GetHaplotypeSet_WhenPatientHasUnrepresentedRegistry_ReturnsGlobalRegistry()
        {
            var donorInfo = DefaultSpecificPopulation;
            var patientInfo = FrequencySetMetadataBuilder.New.ForRegistry("not-recognised").ForEthnicity(DefaultEthnicityCode).Build();

            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.PatientSet.ShouldHaveEquivalentMetadata(GlobalPopulation);
        }

        [Test]
        public async Task GetHaplotypeSet_WhenPatientHasNoRegistryData_ReturnsGlobalRegistry()
        {
            var donorInfo = DefaultSpecificPopulation;
            var patientInfo = FrequencySetMetadataBuilder.New.ForRegistry(null).ForEthnicity(DefaultEthnicityCode).Build();

            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.PatientSet.ShouldHaveEquivalentMetadata(GlobalPopulation);
        }

        [Test]
        public async Task GetHaplotypeSet_WhenPatientHasDifferentRepresentedRegistryToDonor_ReturnsPatientRegistry()
        {
            const string registryCode = "new-registry";
            await ImportHaplotypeSet(registryCode, DefaultEthnicityCode);

            var donorInfo = DefaultSpecificPopulation;
            var patientInfo = FrequencySetMetadataBuilder.New.ForRegistry(registryCode).ForEthnicity(DefaultEthnicityCode).Build();

            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.PatientSet.RegistryCode.Should().Be(patientInfo.RegistryCode);
        }

        [Test]
        public async Task GetHaplotypeSet_WhenDonorAndPatientHaveUnrepresentedEthnicity_ReturnsARegistrySpecificSet()
        {
            var donorInfo = FrequencySetMetadataBuilder.New.ForRegistry(DefaultRegistryCode).ForEthnicity("new-ethnicity-donor").Build();
            var patientInfo = FrequencySetMetadataBuilder.New.ForRegistry(DefaultRegistryCode).ForEthnicity("new-ethnicity-patient").Build();

            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.DonorSet.ShouldHaveEquivalentMetadata(DefaultRegistryOnlyPopulation);
            result.PatientSet.ShouldHaveEquivalentMetadata(DefaultRegistryOnlyPopulation);
        }

        [Test]
        public async Task GetHaplotypeSet_WhenNoEthnicityInformationProvided_ReturnsARegistrySpecificSet()
        {
            var donorInfo = FrequencySetMetadataBuilder.New.ForRegistry(DefaultRegistryCode).Build();
            var patientInfo = FrequencySetMetadataBuilder.New.ForRegistry(DefaultRegistryCode).Build();

            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.DonorSet.ShouldHaveEquivalentMetadata(DefaultRegistryOnlyPopulation);
            result.PatientSet.ShouldHaveEquivalentMetadata(DefaultRegistryOnlyPopulation);
        }

        [Test]
        public async Task GetHaplotypeSet_WhenNoInformationPresent_ReturnsGenericSet()
        {
            var donorInfo = FrequencySetMetadataBuilder.New.Build();
            var patientInfo = FrequencySetMetadataBuilder.New.Build();

            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.DonorSet.ShouldHaveEquivalentMetadata(GlobalPopulation);
            result.PatientSet.ShouldHaveEquivalentMetadata(GlobalPopulation);
        }

        [Test]
        public async Task GetHaplotypeSet_WhenAllInformationUnrepresented_ReturnsGenericSet()
        {
            var donorInfo = FrequencySetMetadataBuilder.New
                .ForRegistry("unrepresented-registry-donor")
                .ForEthnicity("unrepresented-ethnicity-donor")
                .Build();
            var patientInfo = FrequencySetMetadataBuilder.New
                .ForRegistry("unrepresented-registry-patient")
                .ForEthnicity("unrepresented-ethnicity-patient")
                .Build();

            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.DonorSet.ShouldHaveEquivalentMetadata(GlobalPopulation);
            result.PatientSet.ShouldHaveEquivalentMetadata(GlobalPopulation);
        }

        [Test]
        public async Task GetHaplotypeSet_WhenEthnicityIsValidButRegistryIsNot_ReturnsGenericSet()
        {
            var donorInfo = FrequencySetMetadataBuilder.New.ForRegistry("new-registry-donor").ForEthnicity(DefaultEthnicityCode).Build();
            var patientInfo = FrequencySetMetadataBuilder.New.ForRegistry("new-registry-patient").ForEthnicity(DefaultEthnicityCode).Build();

            var result = await service.GetHaplotypeFrequencySets(donorInfo, patientInfo);

            result.DonorSet.ShouldHaveEquivalentMetadata(GlobalPopulation);
            result.PatientSet.ShouldHaveEquivalentMetadata(GlobalPopulation);
        }
        
        [Test]
        public async Task GetSingleHaplotypeSet_WhenExactHaplotypeSetMatch_ReturnsEthnicityAndRegistryMatchedSet()
        {
            var setInfo = DefaultSpecificPopulation;

            var result = await service.GetSingleHaplotypeFrequencySet(setInfo);

            result.ShouldHaveEquivalentMetadata(DefaultSpecificPopulation);
        }

        [Test]
        public async Task GetSingleHaplotypeSet_WhenRegistryMatchesButEthnicityDoesNot_ReturnsRegistrySpecificSet()
        {
            var setInfo = FrequencySetMetadataBuilder.New.ForRegistry(DefaultRegistryCode).ForEthnicity("new-ethnicity-code").Build();

            var result = await service.GetSingleHaplotypeFrequencySet(setInfo);
            
            result.ShouldHaveEquivalentMetadata(DefaultRegistryOnlyPopulation);
        }
        
        [Test]
        public async Task GetSingleHaplotypeSet_WhenRegistryAndEthnicityDoNotMatch_ReturnsGlobalSet()
        {
            var setInfo = FrequencySetMetadataBuilder.New.ForRegistry("new-registry-code").ForEthnicity("new-ethnicity-code").Build();

            var result = await service.GetSingleHaplotypeFrequencySet(setInfo);
            
            result.ShouldHaveEquivalentMetadata(GlobalPopulation);
        }

        private async Task ImportHaplotypeSet(FrequencySetMetadata populationData)
        {
            await ImportHaplotypeSet(populationData.RegistryCode, populationData.EthnicityCode);
        }

        private async Task ImportHaplotypeSet(string registry, string ethnicity)
        {
            using var file = FrequencySetFileBuilder.New(registry, ethnicity).Build();
            await service.ImportFrequencySet(file);
        }
    }

    public static class AssertionExtensions
    {
        public static void ShouldHaveEquivalentMetadata(this HaplotypeFrequencySet haplotypeFrequencySet, FrequencySetMetadata metadata)
        {
            haplotypeFrequencySet.EthnicityCode.Should().Be(metadata.EthnicityCode);
            haplotypeFrequencySet.RegistryCode.Should().Be(metadata.RegistryCode);
        }
    }
}