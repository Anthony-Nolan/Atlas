using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Test.Integration.Resources;
using FluentAssertions;
using NUnit.Framework;
using HaplotypeFrequencySet = Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet.HaplotypeFrequencySet;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.MatchProbability
{
    public class HaplotypeFrequencySetSelection : MatchProbabilityTestsBase
    {
        
        private const string AlleleStringB = "08:182";
            
        private const string SpecificRegistryCode = "specific-registry-code";
        private const string SpecificEthnicityCode = "specific-ethnicity-code";
        
        private static readonly List<HaplotypeFrequency> WrongSetToUse = new List<HaplotypeFrequency>
        {
            DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.1m).Build(),
            DefaultHaplotypeFrequency1.With(h => h.A, GGroupA2).With(h => h.Frequency, 0.1m).Build(),
            DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.1m).With(h => h.B, GGroupB2).Build(),
            DefaultHaplotypeFrequency2.With(h => h.Frequency, 0.1m).Build(),
        };
        
        private static readonly List<HaplotypeFrequency> CorrectSetToUse = new List<HaplotypeFrequency>
        {
            DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.5m).Build(),
            DefaultHaplotypeFrequency1.With(h => h.A, GGroupA2).With(h => h.Frequency, 0.5m).Build(),
            DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.75m).With(h => h.B, GGroupB2).Build(),
            DefaultHaplotypeFrequency2.With(h => h.Frequency, 0.5m).Build(),
        };
        
        private static readonly List<HaplotypeFrequency> AlternateSetToUse = new List<HaplotypeFrequency>
        {
            DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.8m).Build(),
            DefaultHaplotypeFrequency1.With(h => h.A, GGroupA2).With(h => h.Frequency, 0.6m).Build(),
            DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.2m).With(h => h.B, GGroupB2).Build(),
            DefaultHaplotypeFrequency2.With(h => h.Frequency, 0.1m).Build(),
        };


        [Test]
        public async Task CalculateMatchProbability_WhenUsingSpecificRegistryAndEthnicity_UsesSpecificHaplotypeFrequencySet()
        {
            await ImportFrequencies(WrongSetToUse, null, null);
            
            await ImportFrequencies(CorrectSetToUse, SpecificEthnicityCode, SpecificRegistryCode);

            var patientHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.B, LocusPosition.One, $"{Alleles.UnambiguousAlleleDetails.B.Position1.Allele}/{AlleleStringB}").Build();
            var donorHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, GGroupA2, GGroupA2).Build();

            var matchProbabilityInput = new MatchProbabilityInput
            {
                PatientHla = patientHla,
                DonorHla = donorHla,
                HlaNomenclatureVersion = HlaNomenclatureVersion,
                DonorFrequencySetMetadata = new FrequencySetMetadata {EthnicityCode = SpecificEthnicityCode, RegistryCode = SpecificRegistryCode},
                PatientFrequencySetMetadata = new FrequencySetMetadata {EthnicityCode = SpecificEthnicityCode, RegistryCode = SpecificRegistryCode}
            };

            var matchDetails = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.OneMismatchProbability.Percentage.Should().Be(40);
        }
        
        [Test]
        public async Task CalculateMatchProbability_WhenUsingRegistryButNotEthnicityMatchedHaplotypeSet_UsesRegistryOnlyHaplotypeFrequencySet()
        {
            await ImportFrequencies(WrongSetToUse, null, null);
            
            await ImportFrequencies(CorrectSetToUse, null, SpecificRegistryCode);
            
            var patientHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.B, LocusPosition.One, $"{Alleles.UnambiguousAlleleDetails.B.Position1.Allele}/{AlleleStringB}").Build();
            var donorHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, GGroupA2, GGroupA2).Build();

            var matchProbabilityInput = new MatchProbabilityInput
            {
                PatientHla = patientHla,
                DonorHla = donorHla,
                HlaNomenclatureVersion = HlaNomenclatureVersion,
                DonorFrequencySetMetadata = new FrequencySetMetadata {EthnicityCode = "unrepresented-donor-ethnicity", RegistryCode = SpecificRegistryCode},
                PatientFrequencySetMetadata = new FrequencySetMetadata {EthnicityCode =  "unrepresented-patient-ethnicity", RegistryCode = SpecificRegistryCode}
            };

            var matchDetails = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.OneMismatchProbability.Percentage.Should().Be(40);
        }
        
        [Test]
        public async Task CalculateMatchProbability_WhenUsingRegistryAndEthnicityCodesAreNotRepresented_UsesGlobalHaplotypeFrequencySet()
        {
            await ImportFrequencies(WrongSetToUse, SpecificEthnicityCode, null);
            
            await ImportFrequencies(CorrectSetToUse, null, null);
            
            var patientHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.B, LocusPosition.One, $"{Alleles.UnambiguousAlleleDetails.B.Position1.Allele}/{AlleleStringB}").Build();
            var donorHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, GGroupA2, GGroupA2).Build();

            var matchProbabilityInput = new MatchProbabilityInput
            {
                PatientHla = patientHla,
                DonorHla = donorHla,
                HlaNomenclatureVersion = HlaNomenclatureVersion,
                DonorFrequencySetMetadata = new FrequencySetMetadata {EthnicityCode = "unrepresented-donor-ethnicity", RegistryCode = "unrepresented-donor-registry"},
                PatientFrequencySetMetadata = new FrequencySetMetadata {EthnicityCode =  "unrepresented-patient-ethnicity", RegistryCode = "unrepresented-patient-registry"}
            };

            var matchDetails = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.OneMismatchProbability.Percentage.Should().Be(40);
        }

        [Test]
        public async Task CalculateMatchProbability_WhenPatientAndDonorAreUsingDifferentHaplotypeSets_UsesADifferentHlaSetForEach()
        {
            const string patientDonorRegistry = "patient-donor-registry";
            const string donorEthnicity = "donor-ethnicity";
            const string patientEthnicity = "patient-ethnicity";

            await ImportFrequencies(CorrectSetToUse, donorEthnicity, patientDonorRegistry);
            await ImportFrequencies(AlternateSetToUse, patientEthnicity, patientDonorRegistry);
            
            var patientHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.B, LocusPosition.One, $"{Alleles.UnambiguousAlleleDetails.B.Position1.Allele}/{AlleleStringB}").Build();
            var donorHla = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, GGroupA2, GGroupA2).Build();

            var matchProbabilityInput = new MatchProbabilityInput
            {
                PatientHla = patientHla,
                DonorHla = donorHla,
                HlaNomenclatureVersion = HlaNomenclatureVersion,
                DonorFrequencySetMetadata = new FrequencySetMetadata {EthnicityCode = donorEthnicity, RegistryCode = patientDonorRegistry},
                PatientFrequencySetMetadata = new FrequencySetMetadata {EthnicityCode =  patientEthnicity, RegistryCode = patientDonorRegistry}
            };
            
            var matchDetails = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.OneMismatchProbability.Percentage.Should().Be(80);
        }
    }
}