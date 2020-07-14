using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Test.Integration.Resources;
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
            DefaultHaplotypeFrequency1.With(h => h.A, DefaultGGroupA2).With(h => h.Frequency, 0.8m).Build(),
            DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.8m).With(h => h.B, DefaultGGroupB2).Build(),
            DefaultHaplotypeFrequency2.With(h => h.Frequency, 0.1m).Build(),
        };
        
        private static readonly List<HaplotypeFrequency> DefaultHaplotypeFrequencySetOption2 = new List<HaplotypeFrequency>
        {
            DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.75m).Build(),
            DefaultHaplotypeFrequency1.With(h => h.A, DefaultGGroupA2).With(h => h.Frequency, 0.75m).Build(),
            DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.75m).With(h => h.B, DefaultGGroupB2).Build(),
            DefaultHaplotypeFrequency2.With(h => h.Frequency, 0.5m).Build(),
        };
        
        private static readonly List<HaplotypeFrequency> DefaultHaplotypeFrequencySetOption3 = new List<HaplotypeFrequency>
        {
            DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.8m).Build(),
            DefaultHaplotypeFrequency1.With(h => h.A, DefaultGGroupA2).With(h => h.Frequency, 0.6m).Build(),
            DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.2m).With(h => h.B, DefaultGGroupB2).Build(),
            DefaultHaplotypeFrequency2.With(h => h.Frequency, 0.1m).Build(),
        };


        [Test]
        public async Task CalculateMatchProbability_WhenUsingSpecificRegistryAndEthnicity_UsesSpecificHaplotypeFrequencySet()
        {
            await ImportFrequencies(DefaultHaplotypeFrequencySetOption1, null, null);
            
            await ImportFrequencies(DefaultHaplotypeFrequencySetOption2, SpecificRegistryCode, SpecificEthnicityCode);
            var patientHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.B, LocusPosition.One, $"{Alleles.UnambiguousAlleleDetails.B.Position1.Allele}/{AlleleStringB}").Build();
            var donorHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, DefaultGGroupA2, DefaultGGroupA2).Build();

            var matchProbabilityInput = new MatchProbabilityInput
            {
                PatientHla = patientHla,
                DonorHla = donorHla,
                HlaNomenclatureVersion = HlaNomenclatureVersion,
                DonorFrequencySetMetadata = new FrequencySetMetadata {EthnicityCode = SpecificEthnicityCode, RegistryCode = SpecificRegistryCode},
                PatientFrequencySetMetadata = new FrequencySetMetadata {EthnicityCode = SpecificEthnicityCode, RegistryCode = SpecificRegistryCode}
            };

            var matchDetails = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            // We are expecting to use DefaultHaplotypeFrequencySetOption2, which will have a one mismatch probability of 50%.
            matchDetails.OneMismatchProbability.Percentage.Should().Be(50);
        }
        
        [Test]
        public async Task CalculateMatchProbability_WhenUsingRegistryButNotEthnicityMatchedHaplotypeSet_UsesRegistryOnlyHaplotypeFrequencySet()
        {
            await ImportFrequencies(DefaultHaplotypeFrequencySetOption1, null, null);
            await ImportFrequencies(DefaultHaplotypeFrequencySetOption2, SpecificRegistryCode, null);
            

            var patientHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.B, LocusPosition.One, $"{Alleles.UnambiguousAlleleDetails.B.Position1.Allele}/{AlleleStringB}").Build();
            var donorHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, DefaultGGroupA2, DefaultGGroupA2).Build();

            var matchProbabilityInput = new MatchProbabilityInput
            {
                PatientHla = patientHla,
                DonorHla = donorHla,
                HlaNomenclatureVersion = HlaNomenclatureVersion,
                DonorFrequencySetMetadata = new FrequencySetMetadata {EthnicityCode = "unrepresented-donor-ethnicity", RegistryCode = SpecificRegistryCode},
                PatientFrequencySetMetadata = new FrequencySetMetadata {EthnicityCode =  "unrepresented-patient-ethnicity", RegistryCode = SpecificRegistryCode}
            };

            var matchDetails = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            // We are expecting to use DefaultHaplotypeFrequencySetOption2, which will have a one mismatch probability of 50%.
            matchDetails.OneMismatchProbability.Percentage.Should().Be(50);
        }
        
        [Test]
        public async Task CalculateMatchProbability_WhenUsingRegistryAndEthnicityCodesAreNotRepresented_UsesGlobalHaplotypeFrequencySet()
        {
            await ImportFrequencies(DefaultHaplotypeFrequencySetOption1, null, null);
            await ImportFrequencies(DefaultHaplotypeFrequencySetOption2, SpecificEthnicityCode, null);
            
            var patientHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.B, LocusPosition.One, $"{Alleles.UnambiguousAlleleDetails.B.Position1.Allele}/{AlleleStringB}").Build();
            var donorHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, DefaultGGroupA2, DefaultGGroupA2).Build();

            var matchProbabilityInput = new MatchProbabilityInput
            {
                PatientHla = patientHla,
                DonorHla = donorHla,
                HlaNomenclatureVersion = HlaNomenclatureVersion,
                DonorFrequencySetMetadata = new FrequencySetMetadata {EthnicityCode = "unrepresented-donor-ethnicity", RegistryCode = "unrepresented-donor-registry"},
                PatientFrequencySetMetadata = new FrequencySetMetadata {EthnicityCode =  "unrepresented-patient-ethnicity", RegistryCode = "unrepresented-patient-registry"}
            };

            var matchDetails = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            // We are expecting to use DefaultHaplotypeFrequencySetOption1, which will have a one mismatch probability of 11%.
            matchDetails.OneMismatchProbability.Percentage.Should().Be(11);
        }

        [Test]
        public async Task CalculateMatchProbability_WhenPatientAndDonorAreUsingDifferentHaplotypeSets_UsesADifferentHlaSetForEach()
        {
            const string patientDonorRegistry = "patient-donor-registry";
            const string donorEthnicity = "donor-ethnicity";
            const string patientEthnicity = "patient-ethnicity";

            await ImportFrequencies(DefaultHaplotypeFrequencySetOption2, patientDonorRegistry, donorEthnicity);
            await ImportFrequencies(DefaultHaplotypeFrequencySetOption3, patientDonorRegistry, patientEthnicity);
            
            var patientHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.B, LocusPosition.One, $"{Alleles.UnambiguousAlleleDetails.B.Position1.Allele}/{AlleleStringB}").Build();
            var donorHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, DefaultGGroupA2, DefaultGGroupA2).Build();

            var matchProbabilityInput = new MatchProbabilityInput
            {
                PatientHla = patientHla,
                DonorHla = donorHla,
                HlaNomenclatureVersion = HlaNomenclatureVersion,
                DonorFrequencySetMetadata = new FrequencySetMetadata {EthnicityCode = donorEthnicity, RegistryCode = patientDonorRegistry},
                PatientFrequencySetMetadata = new FrequencySetMetadata {EthnicityCode =  patientEthnicity, RegistryCode = patientDonorRegistry}
            };
            
            var matchDetails = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            // This test uses a combination of DefaultHaplotypeFrequencySetOption2 and DefaultHaplotypeFrequencySetOption3
            matchDetails.OneMismatchProbability.Percentage.Should().Be(80);
        }
    }
}