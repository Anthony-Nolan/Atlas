using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Test.Integration.Resources.Alleles;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.MatchProbability
{
    public class HaplotypeFrequencySetSelection : MatchProbabilityTestsBase
    {
        private const string AlleleStringB = "08:182";

        private const string SpecificRegistryCode = "specific-registry-code";
        private const string SpecificEthnicityCode = "specific-ethnicity-code";

        private static readonly List<HaplotypeFrequency> DefaultHaplotypeFrequencySetOption1 = new List<HaplotypeFrequency>
        {
            DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.1m).Build(),
            DefaultHaplotypeFrequency1.With(h => h.A, DefaultGGroups.GetPosition(Locus.A, LocusPosition.Two)).With(h => h.Frequency, 0.8m).Build(),
            DefaultHaplotypeFrequency1.With(h => h.B, DefaultGGroups.GetPosition(Locus.B, LocusPosition.Two)).With(h => h.Frequency, 0.8m).Build(),
            DefaultHaplotypeFrequency2.With(h => h.Frequency, 0.1m).Build(),
        };

        private static readonly List<HaplotypeFrequency> DefaultHaplotypeFrequencySetOption2 = new List<HaplotypeFrequency>
        {
            DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.75m).Build(),
            DefaultHaplotypeFrequency1.With(h => h.A, DefaultGGroups.GetPosition(Locus.A, LocusPosition.Two)).With(h => h.Frequency, 0.75m).Build(),
            DefaultHaplotypeFrequency1.With(h => h.B, DefaultGGroups.GetPosition(Locus.B, LocusPosition.Two)).With(h => h.Frequency, 0.75m).Build(),
            DefaultHaplotypeFrequency2.With(h => h.Frequency, 0.5m).Build(),
        };

        private static readonly List<HaplotypeFrequency> DefaultHaplotypeFrequencySetOption3 = new List<HaplotypeFrequency>
        {
            DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.8m).Build(),
            DefaultHaplotypeFrequency1.With(h => h.A, DefaultGGroups.GetPosition(Locus.A, LocusPosition.Two)).With(h => h.Frequency, 0.6m).Build(),
            DefaultHaplotypeFrequency1.With(h => h.B, DefaultGGroups.GetPosition(Locus.B, LocusPosition.Two)).With(h => h.Frequency, 0.2m).Build(),
            DefaultHaplotypeFrequency2.With(h => h.Frequency, 0.1m).Build(),
        };

        [Test]
        public async Task CalculateMatchProbability_WhenUsingSpecificRegistryAndEthnicity_UsesSpecificHaplotypeFrequencySet()
        {
            await ImportFrequencies(DefaultHaplotypeFrequencySetOption1, null, null);
            await ImportFrequencies(DefaultHaplotypeFrequencySetOption2, SpecificRegistryCode, SpecificEthnicityCode);

            var patientHla = DefaultUnambiguousAllelesBuilder
                .WithDataAt(Locus.B, LocusPosition.One, $"{Alleles.UnambiguousAlleleDetails.B.Position1.Allele}/{AlleleStringB}").Build();
            var donorHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, DefaultGGroups.A).Build();

            var matchProbabilityInput = DefaultInputBuilder
                .WithDonorHla(donorHla)
                .WithPatientHla(patientHla)
                .WithPatientMetadata(new FrequencySetMetadata {EthnicityCode = SpecificEthnicityCode, RegistryCode = SpecificRegistryCode})
                .WithDonorMetadata(new FrequencySetMetadata {EthnicityCode = SpecificEthnicityCode, RegistryCode = SpecificRegistryCode})
                .Build();

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            // We are expecting to use DefaultHaplotypeFrequencySetOption2, which will have a one mismatch probability of 50%.
            matchDetails.MatchProbabilities.OneMismatchProbability.Percentage.Should().Be(50);
        }

        [Test]
        public async Task CalculateMatchProbability_WhenUsingRegistryButNotEthnicityMatchedHaplotypeSet_UsesRegistryOnlyHaplotypeFrequencySet()
        {
            await ImportFrequencies(DefaultHaplotypeFrequencySetOption1, null, null);
            await ImportFrequencies(DefaultHaplotypeFrequencySetOption2, SpecificRegistryCode, null);

            var patientHla = DefaultUnambiguousAllelesBuilder
                .WithDataAt(Locus.B, LocusPosition.One, $"{Alleles.UnambiguousAlleleDetails.B.Position1.Allele}/{AlleleStringB}").Build();
            var donorHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, DefaultGGroups.A).Build();

            var matchProbabilityInput = DefaultInputBuilder
                .WithDonorHla(donorHla)
                .WithPatientHla(patientHla)
                .WithPatientMetadata(new FrequencySetMetadata {
                    EthnicityCode = "unrepresented-patient-ethnicity", RegistryCode = SpecificRegistryCode
                })
                .WithDonorMetadata(new FrequencySetMetadata
                {
                    EthnicityCode = "unrepresented-donor-ethnicity", RegistryCode = SpecificRegistryCode
                })
                .Build();

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            // We are expecting to use DefaultHaplotypeFrequencySetOption2, which will have a one mismatch probability of 50%.
            matchDetails.MatchProbabilities.OneMismatchProbability.Percentage.Should().Be(50);
        }

        [Test]
        public async Task CalculateMatchProbability_WhenUsingRegistryAndEthnicityCodesAreNotRepresented_UsesGlobalHaplotypeFrequencySet()
        {
            await ImportFrequencies(DefaultHaplotypeFrequencySetOption1, null, null);
            await ImportFrequencies(DefaultHaplotypeFrequencySetOption2, SpecificEthnicityCode, null);

            var patientHla = DefaultUnambiguousAllelesBuilder
                .WithDataAt(Locus.B, LocusPosition.One, $"{Alleles.UnambiguousAlleleDetails.B.Position1.Allele}/{AlleleStringB}").Build();
            var donorHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, DefaultGGroups.A.Position2).Build();

            var matchProbabilityInput = DefaultInputBuilder
                .WithDonorHla(donorHla)
                .WithPatientHla(patientHla)
                .WithPatientMetadata(new FrequencySetMetadata
                {
                    EthnicityCode = "unrepresented-patient-ethnicity", RegistryCode = "unrepresented-patient-registry"
                })
                .WithDonorMetadata(new FrequencySetMetadata
                {
                    EthnicityCode = "unrepresented-donor-ethnicity", RegistryCode = "unrepresented-donor-registry"
                })
                .Build();

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            // We are expecting to use DefaultHaplotypeFrequencySetOption1, which will have a one mismatch probability of 11%.
            matchDetails.MatchProbabilities.OneMismatchProbability.Percentage.Should().Be(11);
        }

        [Test]
        public async Task CalculateMatchProbability_WhenPatientAndDonorAreUsingDifferentHaplotypeSets_UsesADifferentHlaSetForEach()
        {
            const string sharedRegistry = "patient-donor-registry";
            const string donorEthnicity = "donor-ethnicity";
            const string patientEthnicity = "patient-ethnicity";

            await ImportFrequencies(DefaultHaplotypeFrequencySetOption2, sharedRegistry, donorEthnicity);
            await ImportFrequencies(DefaultHaplotypeFrequencySetOption3, sharedRegistry, patientEthnicity);

            var patientHla = DefaultUnambiguousAllelesBuilder
                .WithDataAt(Locus.B, LocusPosition.One, $"{Alleles.UnambiguousAlleleDetails.B.Position1.Allele}/{AlleleStringB}")
                .Build();
            var donorHla = DefaultUnambiguousAllelesBuilder
                .WithDataAt(Locus.A, DefaultGGroups.A.Position2)
                .Build();

            var matchProbabilityInput = DefaultInputBuilder
                .WithDonorHla(donorHla)
                .WithPatientHla(patientHla)
                .WithPatientMetadata(new FrequencySetMetadata {EthnicityCode = patientEthnicity, RegistryCode = sharedRegistry})
                .WithDonorMetadata(new FrequencySetMetadata {EthnicityCode = donorEthnicity, RegistryCode = sharedRegistry})
                .Build();

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            // This test uses a combination of DefaultHaplotypeFrequencySetOption2 and DefaultHaplotypeFrequencySetOption3
            matchDetails.MatchProbabilities.OneMismatchProbability.Percentage.Should().Be(80);
        }
    }
}